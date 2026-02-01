using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.ProjectMasterTools.Settings;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/{projectId}/project/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster]
[ApiController]
public class ProjectOperationsController(IProjectSettingsClient client) : ControllerBase
{

    [HttpGet]
    public async Task<ActionResult<ProjectPublishSettingsViewModel>> GetPublishSettings(ProjectIdentification projectId)
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

    [HttpGet]
    public async Task<ActionResult<ProjectContactsSettingsViewModel>> GetContactSettings(ProjectIdentification projectId)
        => Ok(await client.GetContactSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> SaveContactSettings(
        ProjectIdentification projectId,
        ProjectContactsSettingsViewModel model)
    {
        model.ProjectId = projectId;
        await client.SaveContactSettings(model);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<ProjectClaimSettingsViewModel>> GetClaimSettings(ProjectIdentification projectId)
        => Ok(await client.GetClaimSettings(projectId));

    [HttpPost]
    public async Task<ActionResult> SaveClaimSettings(
        ProjectIdentification projectId,
        ProjectClaimSettingsViewModel model)
    {
        model.ProjectId = projectId;
        await client.SaveClaimSettings(model);
        return Ok();
    }
}
