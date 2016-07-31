using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.CommonUI.Models;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Helpers;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class FinancesController : Common.ControllerGameBase
  {
    private IFinanceService FinanceService { get; }

    public FinancesController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IFinanceService financeService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      FinanceService = financeService;
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

    public async Task<ActionResult> MoneySummary(int projectId, string export)
    {
      var project = await ProjectRepository.GetProjectWithFinances(projectId);
      var errorResult = AsMaster(project);
      if (errorResult != null)
      {
        return errorResult;
      }
      var viewModel = project.PaymentTypes.Select(pt => new PaymentTypeSummaryViewModel()
      {
        Name = pt.GetDisplayName(),
        Master = pt.User,
        Total = project.FinanceOperations.Where(fo => fo.PaymentTypeId == pt.PaymentTypeId && fo.Approved).Sum(fo => fo.MoneyAmount)
      }).Where(m => m.Total != 0).OrderByDescending(m => m.Total).ThenBy(m => m.Name);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View(viewModel);
      }
      else
      {
        return await Export(viewModel, "money-summary", exportType.Value);
      }
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
          await FinanceService.CreateCashPaymentType(projectid, CurrentUserId, -paymentTypeId);
        }
        else
        {
          await FinanceService.TogglePaymentActivness(projectid, CurrentUserId, paymentTypeId);
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
        await FinanceService.CreateCustomPaymentType(viewModel.ProjectId, CurrentUserId, viewModel.Name, viewModel.UserId);
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
          ProjectId = projectid
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
        await FinanceService.EditCustomPaymentType(viewModel.ProjectId, CurrentUserId, viewModel.PaymentTypeId, viewModel.Name, viewModel.IsDefault);
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
        await FinanceService.CreateFeeSetting(viewModel.ProjectId, CurrentUserId, viewModel.Fee, viewModel.StartDate);
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
        await FinanceService.DeleteFeeSetting(projectid, CurrentUserId, projectFeeSettingId);
        return RedirectToAction("Setup", new { projectid });
      }
      catch
      {
        //TODO: Message that comment is not added
        return RedirectToAction("Setup", new { projectid });
      }
    }
  }
}