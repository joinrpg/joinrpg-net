using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
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

    [HttpGet, Authorize]
    public async Task<ActionResult> Apis(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project) ?? View(new ApisIndexViewModel(project, CurrentUserId));
    }
  }
}
