using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/tools/[action]")]
public class GameToolsController(IProjectRepository projectRepository, IProjectService projectService) : Common.ControllerGameBase(projectRepository, projectService)
{
    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Apis(int projectId)
    {
        var project = await ProjectRepository.GetProjectAsync(projectId);
        return View(new ApisIndexViewModel(project, CurrentUserId));
    }
}
