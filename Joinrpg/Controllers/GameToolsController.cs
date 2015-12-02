using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Allrpg;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class GameToolsController : Common.ControllerGameBase
  {
    private IAllrpgService AllrpgService { get; }

    public GameToolsController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService, IAllrpgService allrpgService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
      AllrpgService = allrpgService;
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> Apis(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      return AsMaster(project) ?? View(project);
    }

    [HttpGet, Authorize]
    public async Task<ActionResult> AllrpgUpdate(int projectId)
    {
      var project = await ProjectRepository.GetProjectAsync(projectId);
      var errorResult = AsMaster(project, pacl => pacl.IsOwner);
      if (errorResult != null)
      {
        return errorResult;
      }
      return View(new AllrpgUpdateViewModel() { ProjectId = projectId, ProjectName = project.ProjectName });
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<ActionResult> AllrpgUpdate(AllrpgUpdateViewModel model)
    {
      var project = await ProjectRepository.GetProjectAsync(model.ProjectId);
      var errorResult = AsMaster(project, pacl => pacl.IsOwner);
      if (errorResult != null)
      {
        return errorResult;
      }
      model.ProjectName = project.ProjectName;
      try
      {
        model.UpdateResult = string.Join("\n", await AllrpgService.UpdateProject(CurrentUserId, model.ProjectId));
        return View(model);
      }
      catch
      {
        return View(model);
      }

    }
  }
}
