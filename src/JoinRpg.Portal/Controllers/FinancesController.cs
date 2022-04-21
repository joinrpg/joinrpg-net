using System.Data.Entity;
using System.Text;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Exporters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Authorize]
[Route("{projectId}/money/[action]")]
public class FinancesController : ControllerGameBase
{
    private IExportDataService ExportDataService { get; }
    private IFinanceService FinanceService { get; }
    private IUriService UriService { get; }
    private IFinanceReportRepository FinanceReportRepository { get; }

    private IVirtualUsersService VirtualUsers { get; }
    public ICurrentUserAccessor CurrentUserAccessor { get; }
    private readonly ILogger<FinancesController> logger;
    private IUnitOfWork UnitOfWork { get; }

    public FinancesController(
        IUnitOfWork uow,
        IProjectRepository projectRepository,
        IProjectService projectService,
        IExportDataService exportDataService,
        IFinanceService financeService,
        IUriService uriService,
        IFinanceReportRepository financeReportRepository,
        IUserRepository userRepository,
        IVirtualUsersService vpu,
        ICurrentUserAccessor currentUserAccessor,
        ILogger<FinancesController> logger
        )
        : base(projectRepository, projectService, userRepository)
    {
        ExportDataService = exportDataService;
        FinanceService = financeService;
        UriService = uriService;
        FinanceReportRepository = financeReportRepository;
        VirtualUsers = vpu;
        UnitOfWork = uow;
        CurrentUserAccessor = currentUserAccessor;
        this.logger = logger;
    }

    [HttpGet]
    [MasterAuthorize]
    public async Task<ActionResult> Setup(int projectid)
    {
        var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
        return View(new FinanceSetupViewModel(project, CurrentUserId, CurrentUserAccessor.IsAdmin, VirtualUsers.PaymentsUser));
    }

    [HttpGet]
    public async Task<ActionResult> Operations(int projectid, string export)
  => await GetFinanceOperationsList(projectid, export, fo => fo.MoneyFlowOperation && fo.Approved);

    [HttpGet]
    public async Task<ActionResult> Moderation(int projectid, string export)
  => await GetFinanceOperationsList(projectid, export, fo => fo.RequireModeration || (fo.State == FinanceOperationState.Proposed && fo.OperationType == FinanceOperationType.Online));

    [MasterAuthorize]
    private async Task<ActionResult> GetFinanceOperationsList(int projectid, string export, Func<FinanceOperation, bool> predicate)
    {
        var project = await ProjectRepository.GetProjectWithFinances(projectid);
        var viewModel = new FinOperationListViewModel(project, UriService,
            project.FinanceOperations.Where(predicate).ToArray());

        var exportType = ExportTypeNameParserHelper.ToExportType(export);

        if (exportType == null)
        {
            return View("Operations", viewModel);
        }
        else
        {
            var frontend = new FinanceOperationExporter(project, UriService);

            var generator = ExportDataService.GetGenerator(exportType.Value, viewModel.Items, frontend);

            var fileName = project.ProjectName + ": Финансы";

            return File(await generator.Generate(), generator.ContentType,
                Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
        }
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> MoneySummary(int projectId)
    {
        var project = await ProjectRepository.GetProjectWithFinances(projectId);
        if (project == null)
        {
            return NotFound();
        }

        var transfers =
            await FinanceReportRepository.GetAllMoneyTransfers(projectId);

        var payments = project.PaymentTypes
            .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
            .Where(m => m.Total != 0).OrderByDescending(m => m.Total).ToArray();

        var viewModel = new MoneyInfoTotalViewModel(project,
            transfers,
            UriService,
            project.FinanceOperations.ToArray(),
            payments,
            CurrentUserId);

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
                await FinanceService.TogglePaymentActiveness(data.ProjectId, data.PaymentTypeId.Value);
            }
            else
            {
                await FinanceService.CreatePaymentType(new CreatePaymentTypeRequest
                {
                    ProjectId = data.ProjectId,
                    TargetMasterId = data.MasterId,
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
            await FinanceService.CreatePaymentType(new CreatePaymentTypeRequest
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
        var project = await ProjectRepository.GetProjectAsync(projectid);
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
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        var paymentType = project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == viewModel.PaymentTypeId);
        if (paymentType == null)
        {
            return NotFound();
        }

        try
        {
            await FinanceService.EditCustomPaymentType(viewModel.ProjectId, viewModel.PaymentTypeId, viewModel.Name, viewModel.IsDefault);
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch (Exception exc)
        {
            ModelState.AddException(exc);
            return View(viewModel);
        }
    }

    [MasterAuthorize(Permission.CanManageMoney)]
    [HttpPost]
    public async Task<ActionResult> CreateFeeSetting(CreateProjectFeeSettingViewModel viewModel)
    {
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await FinanceService.CreateFeeSetting(new CreateFeeSettingRequest()
            {
                ProjectId = viewModel.ProjectId,
                Fee = viewModel.Fee,
                PreferentialFee = viewModel.PreferentialFee,
                StartDate = viewModel.StartDate,
            });
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch
        {
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    [MasterAuthorize(Permission.CanManageMoney)]
    public async Task<ActionResult> DeleteFeeSetting(int projectid, int projectFeeSettingId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectid);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await FinanceService.DeleteFeeSetting(projectid, projectFeeSettingId);
            return RedirectToAction("Setup", new { projectid });
        }
        catch
        {
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { projectid });
        }
    }

    [HttpGet, AllowAnonymous]
    public async Task<ActionResult> SummaryByMaster(string token, int projectId)
    {
        var project = await ProjectRepository.GetProjectWithFinances(projectId);

        var guid = new Guid(token.FromHexString());

        var acl = project.ProjectAcls.SingleOrDefault(a => a.Token == guid);

        if (acl == null)
        {
            return Content("Unauthorized");
        }

        var masterOperations = project.FinanceOperations.ToArray();

        var masterTransfers = await FinanceReportRepository.GetAllMoneyTransfers(projectId);

        var summary =
            MasterBalanceBuilder.ToMasterBalanceViewModels(masterOperations, masterTransfers, projectId);

        var generator = ExportDataService.GetGenerator(ExportType.Csv, summary,
    new MoneySummaryByMasterExporter(UriService));

        var fileName = project.ProjectName + ": " + "money-summary";

        return File(await generator.Generate(), generator.ContentType,
            Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
    }

    [MasterAuthorize(Permission.CanManageMoney)]
    [HttpPost]
    public async Task<ActionResult> ChangeSettings(FinanceGlobalSettingsViewModel viewModel)
    {
        var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
        if (project == null)
        {
            return NotFound();
        }

        try
        {
            await FinanceService.SaveGlobalSettings(new SetFinanceSettingsRequest
            {
                ProjectId = viewModel.ProjectId,
                WarnOnOverPayment = viewModel.WarnOnOverPayment,
                PreferentialFeeEnabled = viewModel.PreferentialFeeEnabled,
                PreferentialFeeConditions = viewModel.PreferentialFeeConditions,
            });
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
        catch
        {
            //TODO: Message that comment is not added
            return RedirectToAction("Setup", new { viewModel.ProjectId });
        }
    }

    [MasterAuthorize()]
    [HttpGet]
    public async Task<ActionResult> ByMaster(int projectId, int masterId)
    {
        var project = await ProjectRepository.GetProjectWithFinances(projectId);
        var transfers =
            await FinanceReportRepository.GetMoneyTransfersForMaster(projectId, masterId);
        var user = await UserRepository.GetById(masterId);

        var operations = project.FinanceOperations
            .Where(fo => fo.State == FinanceOperationState.Approved).ToArray();

        var payments = project.PaymentTypes
            .Where(pt => pt.UserId == masterId)
            .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
            .Where(m => m.Total != 0).OrderByDescending(m => m.Total).ToArray();


        var viewModel = new MoneyInfoForUserViewModel(project,
            transfers,
            user,
            UriService,
            operations,
            payments,
            CurrentUserId);
        return View(viewModel);
    }

    [HttpGet]
    [AdminAuthorize]
    [ActionName("unfixed-list")]
    public async Task<ActionResult> ListUnfixedPayments(int projectId)
    {
        Project project = await UnitOfWork.GetDbSet<Project>()
            .Include(p => p.ProjectFeeSettings)
            .Include(p => p.ProjectFields)
            .Include(p => p.FinanceOperations)
            .SingleAsync(p => p.ProjectId == projectId);
        ICollection<Claim> claims = await UnitOfWork.GetDbSet<Claim>()
            .Where(c => c.ProjectId == projectId && c.CurrentFee == null)
            .Include(c => c.AccommodationRequest)
            .ToArrayAsync();

        var s = new StringBuilder();
        _ = s.AppendLine($"{"Id",10} {"Paid",10} {"Fee",10}");

        foreach (Claim claim in claims)
        {
            _ = s.AppendLine($"{claim.ClaimId,10}" +
                $" {claim.ClaimBalance(),10}" +
                $" {claim.ClaimTotalFee(),10}");
        }

        return Content(s.ToString(), "text/plain", Encoding.ASCII);
    }

    [HttpGet] //TODO fix this to use POST
    [AdminAuthorize]
    [ActionName("unfixed-fix")]
    public async Task<ActionResult> FixUnfixedPayments(int projectId)
    {
        Project project = await UnitOfWork.GetDbSet<Project>()
            .Include(p => p.ProjectFeeSettings)
            .Include(p => p.ProjectFields)
            .Include(p => p.FinanceOperations)
            .SingleAsync(p => p.ProjectId == projectId);
        ICollection<Claim> claims = await UnitOfWork.GetDbSet<Claim>()
            .Where(c => c.ProjectId == projectId && c.CurrentFee == null)
            .Include(c => c.AccommodationRequest)
            .ToArrayAsync();

        DateTime now = DateTime.UtcNow;
        foreach (Claim claim in claims)
        {
            claim.UpdateClaimFeeIfRequired(now);
        }

        await UnitOfWork.SaveChangesAsync();

        return RedirectToAction("Setup", new { projectId });
    }

    private async Task<FileContentResult> ReturnExportResult(string fileName, IExportGenerator generator) =>
        File(await generator.Generate(), generator.ContentType,
            Path.ChangeExtension(fileName.ToSafeFileName(), generator.FileExtension));
}
