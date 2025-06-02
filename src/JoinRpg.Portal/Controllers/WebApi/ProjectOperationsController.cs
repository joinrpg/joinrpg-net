using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectMasterTools.Settings;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/{projectId}/project/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster]
[ApiController]
public class ProjectOperationsController : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<ProjectPublishSettingsViewModel>> GetPublishSettings(ProjectIdentification projectId, [FromServices] IProjectSettingsClient client)
        => Ok(await client.GetPublishSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> SavePublishSettings(
        ProjectIdentification projectId,
        ProjectPublishSettingsViewModel model,
        [FromServices] IProjectSettingsClient client)
    {
        model.ProjectId = projectId;
        await client.SavePublishSettings(model);
        return Ok();
    }
}
