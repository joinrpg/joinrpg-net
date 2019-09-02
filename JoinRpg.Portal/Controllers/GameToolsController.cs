using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

namespace JoinRpg.Portal.Controllers
{
  public class GameToolsController : Common.ControllerGameBase
  {
        public GameToolsController(IProjectRepository projectRepository,
          IProjectService projectService, IUserRepository userRepository)
          : base(projectRepository, projectService, userRepository)
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
