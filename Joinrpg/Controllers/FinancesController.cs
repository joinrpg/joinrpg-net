using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
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

    public FinancesController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }

    public async Task<ActionResult> List(int projectid)
    {

      var project = await ProjectRepository.GetProjectWithFinances(projectid);
      var errorResult = AsMaster(project);
      if (errorResult != null)
      {
        return errorResult;
      }
      return
        View(
          project.FinanceOperations.Where(fo => !fo.RequireModeration)
            .OrderBy(f => f.OperationDate)
            .Select(FinOperationListItemViewModel.Create));
    }
  }
}