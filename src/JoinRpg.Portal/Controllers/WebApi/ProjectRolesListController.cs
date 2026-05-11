using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

[Route("/webapi/project-roles-list/[action]")]
[RequireMaster]
[IgnoreAntiforgeryToken]
public class ProjectRolesListController(IProjectRolesListClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId)
        => await client.GetList(projectId);

    [HttpPost]
    [RequireMaster(Permission.CanEditRoles)]
    public async Task<ActionResult> Remove([FromQuery] ProjectIdentification projectId, [FromBody] ProjectRolesListIdentification id)
    {
        if (id.ProjectId != projectId)
        {
            return BadRequest();
        }
        await client.Remove(id);
        return Ok();
    }

    [HttpPost]
    [RequireMaster(Permission.CanEditRoles)]
    public async Task<ActionResult<ProjectRolesListViewModel>> AddOrChange([FromQuery] ProjectIdentification projectId, [FromBody] DomainTypes.ProjectMetadata.ProjectRolesList model)
    {
        if (model.ProjectRolesListId.ProjectId != projectId)
        {
            return BadRequest();
        }
        return Ok(await client.AddOrChange(model));
    }
}
