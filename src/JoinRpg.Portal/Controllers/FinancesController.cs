using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Helpers;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Access;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Exporters;
using JoinRpg.Web.Models.Money;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/money/[action]")]
public class FinancesController(
    IProjectRepository projectRepository,
    IExportDataService exportDataService,
    IFinanceService financeService,
    IUriService uriService,
    IFinanceReportRepository financeReportRepository,
    IUserRepository userRepository,
    IVirtualUsersService vpu,
    ICurrentUserAccessor currentUserAccessor,
    ILogger<FinancesController> logger,
    IProjectMetadataRepository projectMetadataRepository
        ) : JoinControllerGameBase
{
    protected readonly IUserRepository UserRepository = userRepository;

    [HttpGet]
    [MasterAuthorize]
    public async Task<ActionResult> Setup(int projectid)
    {
        var project = await projectRepository.GetProjectForFinanceSetup(projectid);
        return View(new FinanceSetupViewModel(project, currentUserAccessor.UserId, currentUserAccessor.IsAdmin, vpu.PaymentsUser));
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
        var viewModel = new FinOperationListViewModel(projectid, uriService,
            project.FinanceOperations.Where(predicate).ToArray());

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            return View("Operations", viewModel);
        }
        else
        {
            var metadata = await projectMetadataRepository.GetProjectMetadata(new(projectid));
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

        var payments = project.PaymentTypes
            .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
            .Where(m => m.Total != 0).OrderByDescending(m => m.Total).ToArray();

        var viewModel = new MoneyInfoTotalViewModel(projectInfo,
            transfers,
            uriService,
            project.FinanceOperations.ToArray(),
            payments,
            currentUserAccessor);

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
                await financeService.TogglePaymentActiveness(data.ProjectId, data.PaymentTypeId.Value);
            }
            else
            {
                await financeService.CreatePaymentType(new CreatePaymentTypeRequest
                {
                    ProjectId = data.ProjectId,
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
        try
        {
            await financeService.CreatePaymentType(new CreatePaymentTypeRequest
            {
                ProjectId = viewModel.ProjectId,
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
        var project = await projectRepository.GetProjectAsync(projectid);
        var paymentType = project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == paymenttypeid);
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
        var project = await projectRepository.GetProjectAsync(viewModel.ProjectId);
        var paymentType = project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == viewModel.PaymentTypeId);
        if (paymentType == null)
        {
            return NotFound();
        }

        try
        {
            await financeService.EditCustomPaymentType(viewModel.ProjectId, viewModel.PaymentTypeId, viewModel.Name, viewModel.IsDefault);
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception exc)
        {
            AddModelException(exc);
            return View(viewModel);
        }
    }

    [MasterAuthorize(Permission.CanManageMoney)]
    [HttpPost]
    public async Task<ActionResult> CreateFeeSetting(CreateProjectFeeSettingViewModel viewModel)
    {
        var project = await projectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await financeService.CreateFeeSetting(new CreateFeeSettingRequest()
            {
                ProjectId = viewModel.ProjectId,
                Fee = viewModel.Fee,
                PreferentialFee = viewModel.PreferentialFee,
                StartDate = viewModel.StartDate,
            });
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanManageMoney)]
    public async Task<ActionResult> DeleteFeeSetting(int projectid, int projectFeeSettingId)
    {
        var project = await projectRepository.GetProjectAsync(projectid);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await financeService.DeleteFeeSetting(projectid, projectFeeSettingId);
            return RedirectToAction("Setup", new { projectid });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { projectid });
        }
    }

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> SummaryByMaster(string token, int projectId)
    {
        var project = await projectRepository.GetProjectWithFinances(projectId);

        var guid = new Guid(Convert.FromHexString(token));

        var acl = project.ProjectAcls.SingleOrDefault(a => a.Token == guid);

        if (acl == null)
        {
            return Content("Unauthorized");
        }

        var masterOperations = project.FinanceOperations.ToArray();

        var masterTransfers = await financeReportRepository.GetAllMoneyTransfers(projectId);

        var summary =
            MasterBalanceBuilder.ToMasterBalanceViewModels(masterOperations, masterTransfers, projectId);

        var generator = exportDataService.GetGenerator(ExportType.Csv, summary,
    new MoneySummaryByMasterExporter(uriService));

        return GeneratorResultHelper.Result(project.ProjectName + ": Финансы", generator);
    }

    [MasterAuthorize(Permission.CanManageMoney)]
    [HttpPost]
    public async Task<ActionResult> ChangeSettings(FinanceGlobalSettingsViewModel viewModel)
    {
        var project = await projectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await financeService.SaveGlobalSettings(new SetFinanceSettingsRequest
            {
                ProjectId = viewModel.ProjectId,
                WarnOnOverPayment = viewModel.WarnOnOverPayment,
                PreferentialFeeEnabled = viewModel.PreferentialFeeEnabled,
                PreferentialFeeConditions = viewModel.PreferentialFeeConditions,
            });
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка");
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> ByMaster(ProjectIdentification projectId, int masterId)
    {
        var project = await projectRepository.GetProjectWithFinances(projectId);
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        var transfers =
            await financeReportRepository.GetMoneyTransfersForMaster(projectId, masterId);
        var user = await UserRepository.GetById(masterId);

        var operations = project.FinanceOperations
            .Where(fo => fo.State == FinanceOperationState.Approved)
            .Where(fo => fo.PaymentType?.UserId == masterId)
            .ToArray();

        var payments = project.PaymentTypes
            .Where(pt => pt.UserId == masterId)
            .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
            .Where(m => m.Total != 0)
            .OrderByDescending(m => m.Total)
            .ToArray();


        var viewModel = new MoneyInfoForUserViewModel(transfers,
            user,
            uriService,
            operations,
            payments,
            currentUserAccessor,
            projectInfo);
        return View(viewModel);
    }
}
