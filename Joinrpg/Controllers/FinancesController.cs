using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
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
      => await GetFinanceOperationsList(projectid, export, fo => !fo.RequireModeration);

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
      var viewModel = project.FinanceOperations.Where(predicate)
        .OrderBy(f => f.OperationDate)
        .Select(FinOperationListItemViewModel.Create);

      var exportType = GetExportTypeByName(export);

      if (exportType == null)
      {
        return View("Operations", viewModel);
      }
      else
      {
        return await Export(viewModel, "finance-export", exportType.Value);
      }
    }
  }
}