using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  [Authorize]
  public class FinancesController : Common.ControllerGameBase
  {
    
    public async Task<ActionResult> Setup(int projectid)
    {
      return AsMaster(await ProjectRepository.GetProjectAsync(projectid)) ?? View();
    }

    public FinancesController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
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
  }
}