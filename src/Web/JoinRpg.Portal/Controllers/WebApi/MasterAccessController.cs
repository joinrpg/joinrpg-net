using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Acl;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Добавление мастера и правка его прав (ADR019, §4) — с правом выдавать доступ.
/// </summary>
[Route("/webapi/{projectId}/master-access/[action]")]
[IgnoreAntiforgeryToken]
[RequireMaster(Permission.CanGrantRights)]
[ApiController]
public class MasterAccessController(IMasterAccessClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AddMasterViewModel>> GetAddMaster(ProjectIdentification projectId, [FromQuery] UserIdentification userId)
        => Ok(await client.GetAddMaster(projectId, userId));

    [HttpPost]
    public async Task<ActionResult> AddMaster(ProjectIdentification projectId, AddMasterViewModel model)
    {
        model.ProjectId = projectId;
        await client.AddMaster(model);
        return Ok();
    }

    [HttpGet]
    public async Task<ActionResult<MasterPermissionsViewModel>> GetPermissions(ProjectIdentification projectId, [FromQuery] UserIdentification userId)
        => Ok(await client.GetPermissions(projectId, userId));

    [HttpPost]
    public async Task<ActionResult> SavePermissions(ProjectIdentification projectId, MasterPermissionsViewModel model)
    {
        model.ProjectId = projectId;
        await client.SavePermissions(model);
        return Ok();
    }
}
