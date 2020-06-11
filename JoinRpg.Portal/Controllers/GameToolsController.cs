using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [Route("{projectId}/tools/[action]")]
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
