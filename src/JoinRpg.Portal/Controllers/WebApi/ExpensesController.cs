using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Finances.Expenses;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Расходы проекта (ADR030). Смотреть может любой мастер; создавать — любой мастер;
/// править — мастер свои расходы или созданные им (проверяет сервис); одобрять/отклонять —
/// только с <see cref="Permission.CanManageMoney"/>.
/// </summary>
[Route("/webapi/{projectId}/expenses/[action]")]
[RequireMaster]
[ApiController]
public class ExpensesController(
    IExpenseRepository expenseRepository,
    IExpenseService expenseService,
    ICurrentUserAccessor currentUserAccessor,
    IProjectMetadataRepository projectMetadataRepository,
    IExportDataService exportDataService,
    IUriService uriService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseViewModel>>> List(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return Ok(ToDtos(projectInfo, await expenseRepository.GetExpenses(projectId)));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExpenseViewModel>>> My(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return Ok(ToDtos(projectInfo, await expenseRepository.GetExpensesByMaster(projectId, currentUserAccessor.UserIdentification)));
    }

    /// <summary>Может ли текущий мастер одобрять и отклонять расходы — для кнопок на странице.</summary>
    [HttpGet]
    public async Task<ActionResult<bool>> CanManageMoney(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return Ok(projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney));
    }

    /// <summary>Полные пути категорий с не отклонёнными расходами и их предков — подсказки поля «Категория».</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<string>>> CategoryPathSuggestions(ProjectIdentification projectId)
        => Ok(await expenseService.LoadCategoryPathSuggestions(projectId));

    /// <summary>Текущий пользователь — мастер по умолчанию в форме нового расхода.</summary>
    [HttpGet]
    public ActionResult<UserIdentification> CurrentUser(ProjectIdentification projectId)
        => Ok(currentUserAccessor.UserIdentification);

    /// <summary>Выгрузка расходов проекта в Excel/CSV — зеркало грида страницы «Расходы».</summary>
    [HttpGet]
    public async Task<ActionResult> Export(ProjectIdentification projectId, string? export)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var expenses = await expenseRepository.GetExpenses(projectId);
        var exportType = ExportTypeNameParserHelper.ToExportType(export) ?? ExportType.ExcelXml;
        var masterNames = MasterNames(projectInfo);
        var items = expenses.Select(expense => new ExpenseExportItem(
            expense.ExpenseId.ExpenseId,
            string.Join("\\", expense.CategoryPath.Select(segment => segment.Name)),
            expense.Name,
            masterNames[expense.MasterId],
            expense.PlannedQuantity,
            expense.PlannedPrice,
            expense.ActualQuantity,
            expense.ActualPrice,
            expense.State.ToView().GetDisplayName(),
            expense.SpentAt,
            expense.Description)).ToArray();
        var generator = exportDataService.GetGenerator(exportType, items, new ExpenseExporter(uriService));
        return GeneratorResultHelper.Result(projectInfo.ProjectName.Value + ": Расходы", generator);
    }

    [HttpPost]
    public async Task<ActionResult<ExpenseIdentification>> Create(ProjectIdentification projectId, ExpenseEditViewModel model)
    {
        var id = await expenseService.CreateExpense(projectId, new CreateExpenseRequest(
            model.MasterId!,
            model.Name,
            model.CategoryPath,
            model.PlannedQuantity,
            model.PlannedPrice,
            model.SpentAt,
            ToMarkdown(model.Description),
            model.ActualQuantity,
            model.ActualPrice));
        // Финансовый администратор одобряет свой расход молча — отдельным вызовом (отдельная транзакция)
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
        {
            await expenseService.ApproveExpense(id, comment: null);
        }
        return Ok(id);
    }

    // expenseId биндится типизированным: голое число смёрживается биндером с projectId маршрута
    // и проверяется на совпадение проекта (ProjectEntityIdModelBinder).
    [HttpPost]
    public async Task<ActionResult> Edit(ProjectIdentification projectId, [FromQuery] ExpenseIdentification expenseId, ExpenseEditViewModel model)
    {
        var approvalReset = await expenseService.EditExpense(expenseId, new EditExpenseRequest(
            model.MasterId!,
            model.Name,
            model.CategoryPath,
            model.PlannedQuantity,
            model.PlannedPrice,
            model.SpentAt,
            ToMarkdown(model.Description),
            model.ActualQuantity,
            model.ActualPrice));
        // Одобрение сброшено правкой, а редактирующий — финансовый администратор:
        // восстанавливаем одобрение отдельным вызовом (отдельная транзакция), как при создании
        if (approvalReset)
        {
            var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
            if (projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
            {
                await expenseService.ApproveExpense(expenseId, comment: null);
            }
        }
        return Ok();
    }

    [HttpPost]
    public async Task<ActionResult> SetActual(ProjectIdentification projectId, [FromQuery] ExpenseIdentification expenseId, ExpenseActualRequest model)
    {
        await expenseService.SetExpenseActual(expenseId, model);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageMoney)]
    public async Task<ActionResult> Approve(ProjectIdentification projectId, [FromQuery] ExpenseIdentification expenseId, ExpenseCommentRequest? comment)
    {
        await expenseService.ApproveExpense(expenseId, ToMarkdown(comment?.Comment));
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanManageMoney)]
    public async Task<ActionResult> Reject(ProjectIdentification projectId, [FromQuery] ExpenseIdentification expenseId, ExpenseCommentRequest? comment)
    {
        await expenseService.RejectExpense(expenseId, ToMarkdown(comment?.Comment));
        return Ok();
    }

    private static MarkdownString? ToMarkdown(string? markdown)
        => string.IsNullOrWhiteSpace(markdown) ? null : new MarkdownString(markdown);

    private static ExpenseViewModel ToDto(ExpenseInfo expense, Dictionary<UserIdentification, string> masterNames)
        => new(
            expense.ExpenseId,
            expense.Name,
            string.Join("\\", expense.CategoryPath.Select(segment => segment.Name)),
            expense.PlannedQuantity,
            expense.PlannedPrice,
            expense.ActualQuantity,
            expense.ActualPrice,
            expense.SpentAt,
            expense.State.ToView(),
            masterNames[expense.MasterId],
            masterNames[expense.CreatedById],
            expense.Description?.Value,
            expense.MasterId);

    private static Dictionary<UserIdentification, string> MasterNames(ProjectInfo projectInfo)
        // Имена — из метаданных проекта: UserInfoHeader есть у каждого мастера, включая бывших.
        => projectInfo.Masters
            .Concat(projectInfo.FormerMasters)
            .ToDictionary(master => master.UserId, master => master.UserInfo.DisplayName.DisplayName);

    private static ExpenseViewModel[] ToDtos(ProjectInfo projectInfo, IReadOnlyCollection<ExpenseInfo> expenses)
    {
        var masterNames = MasterNames(projectInfo);
        return expenses.Select(expense => ToDto(expense, masterNames)).ToArray();
    }
}

/// <summary>Опциональный комментарий к решению по расходу (markdown).</summary>
public record ExpenseCommentRequest(string? Comment);
