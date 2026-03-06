using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Controllers.Common;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[Route("{projectId}/tools/[action]")]
public class GameToolsController(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : JoinControllerGameBase
{
    [HttpGet, MasterAuthorize()]
    public async Task<ActionResult> Apis(int projectId)
    {
        var project = await projectRepository.GetProjectAsync(projectId);
        return View(new ApisIndexViewModel(project, currentUserAccessor.UserId));
    }
}
