using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Exporters;
using JoinRpg.Web.Models.Money;
using JoinRpg.Web.UserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/money/[action]")]
public class FinancesController(
    IProjectRepository projectRepository,
    IExportDataService exportDataService,
    IProjectFinanceSettingsService financeSettingsService,
    IUriService uriService,
    IFinanceReportRepository financeReportRepository,
    IUserRepository userRepository,
    IVirtualUsersService vpu,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<FinancesController> logger,
    IProjectMetadataRepository projectMetadataRepository,
    IClaimsRepository claimsRepository,
    UserProfileDetailsBuilder cardBuilder
        ) : JoinControllerGameBase
{
    protected readonly IUserRepository UserRepository = userRepository;

    [HttpGet]
    [MasterAuthorize]
    public async Task<ActionResult> Setup(int projectid) => await ShowSetup(projectid);

    private async Task<ActionResult> ShowSetup(int projectid)
    {
        var project = await projectRepository.GetProjectForFinanceSetup(projectid);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        return View("Setup", new FinanceSetupViewModel(project, projectInfo, currentUserAccessor.UserId, currentUserAccessor.IsAdmin, vpu.PaymentsUser.ToUserInfoHeader()));
    }

    [HttpGet]
    [RequireMaster]
    public async Task<ActionResult> Operations(ProjectIdentification projectid, string export)
  => await GetFinanceOperationsList(projectid, export, fo => fo.MoneyFlowOperation && fo.Approved);

    [HttpGet]
    [RequireMaster]
    public async Task<ActionResult> Moderation(ProjectIdentification projectid, string export)
  => await GetFinanceOperationsList(projectid, export, fo => fo.RequireModeration || (fo.State == FinanceOperationState.Proposed && fo.OperationType == FinanceOperationType.Online));

    private async Task<ActionResult> GetFinanceOperationsList(ProjectIdentification projectid, string export, Func<FinanceOperation, bool> predicate)
    {
        var project = await projectRepository.GetProjectWithFinances(projectid);
        var metadata = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var operations = project.FinanceOperations.Where(predicate).ToArray();
        var exportType = ExportTypeNameParserHelper.ToExportType(export);
        var viewModel = new FinOperationListViewModel(metadata, uriService,
            operations,
            await LoadOperationListData(operations, withPlayerProfiles: exportType is not null),
            currentUserAccessor.UserIdentificationOrDefault);

        if (exportType == null)
        {
            return View("Operations", viewModel);
        }
        else
        {
            var frontend = new FinanceOperationExporter(uriService, metadata);

            var generator = exportDataService.GetGenerator(exportType.Value, viewModel.Items, frontend);

            return GeneratorResultHelper.Result(project.ProjectName + ": Финансы", generator);
        }
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> MoneySummary(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var project = await projectRepository.GetProjectWithFinances(projectId);
        if (project == null)
        {
            return NotFound();
        }

        var transfers =
            await financeReportRepository.GetAllMoneyTransfers(projectId);

        var operations = project.FinanceOperations.ToArray();

        var payments = projectInfo.ProjectFinanceSettings.PaymentTypes
            .Select(pt => new PaymentTypeSummaryViewModel(pt, operations))
            .Where(m => m.Total != 0).OrderByDescending(m => m.Total).ToArray();

        var masterIds = MasterBalanceBuilder.GetMasterIds(operations, transfers, projectInfo);
        var masters = await UserRepository.GetRequiredUserInfos(masterIds);

        var viewModel = new MoneyInfoTotalViewModel(projectInfo,
            transfers,
            operations,
            payments,
            currentUserAccessor,
            masters);

        return View(viewModel);
    }

    [HttpPost]
    [RequireMasterOrAdmin(Permission.CanManageMoney)]
    public async Task<ActionResult> TogglePaymentType(TogglePaymentTypeViewModel data)
    {
        try
        {
            if (data.PaymentTypeId > 0)
            {
                await financeSettingsService.TogglePaymentActiveness(new PaymentTypeIdentification(data.ProjectId, data.PaymentTypeId.Value));
            }
            else
            {
                await financeSettingsService.CreatePaymentType(new CreatePaymentTypeRequest
                {
                    ProjectId = new(data.ProjectId),
                    TargetMasterId = data.MasterId,
                    Name = null, // У них специальное имя
                    TypeKind = (PaymentTypeKind)data.TypeKind.GetValueOrDefault(PaymentTypeKindViewModel.Custom),
                });
            }

            return RedirectToAction("Setup", new { projectid = data.ProjectId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Проблема при включении платежа");
            //TODO: Message that payment type was not created
            return RedirectToAction("Setup", new { projectid = data.ProjectId });
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequireMasterOrAdmin(Permission.CanManageMoney)]
    public async Task<ActionResult> CreatePaymentType(CreatePaymentTypeViewModel viewModel)
    {
        // Форма — модальный диалог на странице настроек: ошибку показываем сводкой над страницей.
        if (viewModel.UserId is null)
        {
            ModelState.AddModelError("", "Не удалось добавить способ оплаты: выберите мастера");
            return await ShowSetup(viewModel.ProjectId);
        }

        try
        {
            await financeSettingsService.CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = new(viewModel.ProjectId),
                TargetMasterId = viewModel.UserId,
                TypeKind = PaymentTypeKind.Custom,
                Name = viewModel.Name,
            });
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Проблема при создании платежа");
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
    }

    [HttpGet]
    [MasterAuthorize(Permission.CanManageMoney)]
    public async Task<ActionResult> EditPaymentType(int projectid, int paymenttypeid)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectid));
        var paymentType = projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(new PaymentTypeIdentification(projectid, paymenttypeid));
        if (paymentType == null)
        {
            return NotFound();
        }
        return
          View(new EditPaymentTypeViewModel()
          {
              IsDefault = paymentType.IsDefault,
              Name = paymentType.Name,
              ProjectId = projectid,
          });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanManageMoney)]
    public async Task<ActionResult> EditPaymentType(EditPaymentTypeViewModel viewModel)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(viewModel.ProjectId));
        var paymentType = projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(new PaymentTypeIdentification(viewModel.ProjectId, viewModel.PaymentTypeId));
        if (paymentType == null)
        {
            return NotFound();
        }

        try
        {
            await financeSettingsService.EditCustomPaymentType(new PaymentTypeIdentification(viewModel.ProjectId, viewModel.PaymentTypeId), viewModel.Name, viewModel.IsDefault);
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception exc)
        {
            AddModelException(exc);
            return View(viewModel);
        }
    }

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> SummaryByMaster(string token, int projectId)
    {
        var project = await projectRepository.GetProjectWithFinances(projectId);

        var guid = new Guid(Convert.FromHexString(token));

        // Токен снятого мастера больше не открывает сводку (ADR019, §1).
        var acl = project.ProjectAcls.SingleOrDefault(a => a.Token == guid && a.IsActive);

        if (acl == null)
        {
            return Content("Unauthorized");
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        var masterOperations = project.FinanceOperations.ToArray();

        var masterTransfers = await financeReportRepository.GetAllMoneyTransfers(projectId);

        var masterIds = MasterBalanceBuilder.GetMasterIds(masterOperations, masterTransfers, projectInfo);
        var masters = await UserRepository.GetRequiredUserInfos(masterIds);

        var summary =
            MasterBalanceBuilder.ToMasterBalanceViewModels(masters, masterOperations, masterTransfers, projectInfo);

        var generator = exportDataService.GetGenerator(ExportType.Csv, summary,
    new MoneySummaryByMasterExporter(uriService));

        return GeneratorResultHelper.Result(project.ProjectName + ": Финансы", generator);
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> ByMaster(ProjectIdentification projectId, int masterId)
    {
        var project = await projectRepository.GetProjectWithFinances(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var transfers =
            await financeReportRepository.GetMoneyTransfersForMaster(projectId, masterId);
        var user = await UserRepository.GetRequiredUserInfo(new UserIdentification(masterId));

        var operations = project.FinanceOperations
            .Where(fo => fo.State == FinanceOperationState.Approved)
            .Where(fo => fo.GetPaymentMasterId(projectInfo) == user.UserId)
            .ToArray();

        var payments = projectInfo.ProjectFinanceSettings.PaymentTypes
            .Where(pt => pt.User.UserId == user.UserId)
            .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
            .Where(m => m.Total != 0)
            .OrderByDescending(m => m.Total)
            .ToArray();


        var viewModel = new MoneyInfoForUserViewModel(transfers,
            user,
            uriService,
            operations,
            await LoadOperationListData(operations, withPlayerProfiles: false),
            payments,
            currentUserAccessor,
            projectInfo,
            cardBuilder);
        return View(viewModel);
    }

    /// <summary>
    /// Заголовки заявок с игроками и авторы операций — пачкой на весь список, а не навигациями каждой операции (#5852).
    /// Полные профили игроков с контактами — только для выгрузки.
    /// </summary>
    private async Task<FinOperationListData> LoadOperationListData(IReadOnlyCollection<FinanceOperation> operations, bool withPlayerProfiles)
    {
        var claims = await claimsRepository.GetClaimHeadersWithPlayer(
            [.. operations.Select(fo => new ClaimIdentification(fo.ProjectId, fo.ClaimId)).Distinct()]);
        var authors = await UserRepository.GetRequiredUserInfoHeaders(
            [.. operations.Select(fo => new UserIdentification(fo.Comment.AuthorUserId)).Distinct()]);
        var playerProfiles = withPlayerProfiles
            ? (await UserRepository.GetRequiredUserInfos([.. claims.Select(c => c.Player.UserId).Distinct()])).ToDictionary(u => u.UserId)
            : null;
        return new FinOperationListData(
            claims.ToDictionary(c => c.ClaimId),
            authors.ToDictionary(u => u.UserId),
            playerProfiles);
    }
}
