using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.CommonUI.Models;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Helpers;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.Exporters;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class FinancesController : Common.ControllerGameBase
  {
      private IFinanceService FinanceService { get; }
      private IUriService UriService { get; }
      private IFinanceReportRepository FinanceReportRepository { get; }
      private IUserRepository UserRepository { get; }

      public FinancesController(ApplicationUserManager userManager,
          IProjectRepository projectRepository,
          IProjectService projectService,
          IExportDataService exportDataService,
          IFinanceService financeService,
          IUriService uriService,
          IFinanceReportRepository financeReportRepository,
          IUserRepository userRepository)
          : base(userManager, projectRepository, projectService, exportDataService)
      {
          FinanceService = financeService;
          UriService = uriService;
          FinanceReportRepository = financeReportRepository;
          UserRepository = userRepository;
      }

      [HttpGet]
    public async Task<ActionResult> Setup(int projectid)
    {
      var project = await ProjectRepository.GetProjectForFinanceSetup(projectid);
      return AsMaster(project) ?? View(new FinanceSetupViewModel(project, CurrentUserId));
    }

    public async Task<ActionResult> Operations(int projectid, string export)
      => await GetFinanceOperationsList(projectid, export, fo => fo.Approved);

    public async Task<ActionResult> Moderation(int projectid, string export)
      => await GetFinanceOperationsList(projectid, export, fo => fo.RequireModeration);

    private async Task<ActionResult> GetFinanceOperationsList(int projectid, string export, Func<FinanceOperation, bool> predicate)
    {
      var project = await ProjectRepository.GetProjectWithFinances(projectid);
      var errorResult = AsMaster(project);
      if (errorResult != null)
      {
        return errorResult;
      }
      var viewModel = new FinOperationListViewModel(project, new UrlHelper(ControllerContext.RequestContext),
        project.FinanceOperations.Where(predicate).ToArray());

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View("Operations", viewModel);
      }
      else
      {
        return await Export(viewModel.Items, "finance-export", exportType.Value);
      }
    }

      [MasterAuthorize()]
      public async Task<ActionResult> MoneySummary(int projectId)
      {
          var project = await ProjectRepository.GetProjectWithFinances(projectId);
          if (project == null)
          {
              return HttpNotFound();
          }

          var transfers =
              await FinanceReportRepository.GetAllMoneyTransfers(projectId);

          var payments = project.PaymentTypes
              .Select(pt => new PaymentTypeSummaryViewModel(pt, project.FinanceOperations))
              .Where(m => m.Total != 0).OrderByDescending(m => m.Total).ToArray();

          var viewModel = new MoneyInfoTotalViewModel(project,
              transfers,
              new UrlHelper(ControllerContext.RequestContext),
              project.FinanceOperations.ToArray(),
              payments,
              CurrentUserId);

          return View(viewModel);
      }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> TogglePaymentType(int projectid, int paymentTypeId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var errorResult = AsMaster(project, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
      }

      try
      {
        if (paymentTypeId < 0)
        {
          await FinanceService.CreateCashPaymentType(projectid, -paymentTypeId);
        }
        else
        {
          await FinanceService.TogglePaymentActivness(projectid, paymentTypeId);
        }
        return RedirectToAction("Setup", new { projectid });
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Setup", new { projectid });
      }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<ActionResult> CreatePaymentType(CreatePaymentTypeViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
      }

      try
      {
        await FinanceService.CreateCustomPaymentType(viewModel.ProjectId, viewModel.Name, viewModel.UserId);
        return RedirectToAction("Setup", new { viewModel.ProjectId });
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Setup", new { viewModel.ProjectId });
      }
    }

    [HttpGet]
    public async Task<ActionResult> EditPaymentType(int projectid, int paymenttypeid)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var paymentType = project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == paymenttypeid);
      var errorResult = AsMaster(paymentType, acl => acl.CanManageMoney);
      if (errorResult != null || paymentType == null)
      {
        return errorResult;
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
    public async Task<ActionResult> EditPaymentType(EditPaymentTypeViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var paymentType = project.PaymentTypes.SingleOrDefault(pt => pt.PaymentTypeId == viewModel.PaymentTypeId);
      var errorResult = AsMaster(paymentType, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
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

    public async Task<ActionResult> CreateFeeSetting(CreateProjectFeeSettingViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
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

    [HttpPost,ValidateAntiForgeryToken]
    public async Task<ActionResult> DeleteFeeSetting(int projectid, int projectFeeSettingId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectid);
      var errorResult = AsMaster(project, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
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

          return
              await
                  ExportWithCustomFronend(summary,
                      "money-summary",
                      ExportType.Csv,
                      new MoneySummaryByMasterExporter(UriService),
                      project.ProjectName);
      }

    public async Task<ActionResult> ChangeSettings(FinanceGlobalSettingsViewModel viewModel)
    {
      var project = await ProjectRepository.GetProjectAsync(viewModel.ProjectId);
      var errorResult = AsMaster(project, acl => acl.CanManageMoney);
      if (errorResult != null)
      {
        return errorResult;
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
              new UrlHelper(ControllerContext.RequestContext),
              operations,
              payments,
              CurrentUserId);
          return View(viewModel);
      }
  }
}
