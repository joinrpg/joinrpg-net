using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameToolsController : Common.ControllerGameBase
  {
    public GameToolsController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
    }

    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Apis(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return View(new ApisIndexViewModel(project, CurrentUserId));
    }
  }
}
