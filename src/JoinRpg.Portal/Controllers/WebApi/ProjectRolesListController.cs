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

    [HttpGet]
    public async Task<ProjectRolesList> GetById(ProjectRolesListIdentification targetId) => await client.GetById(targetId);

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
    public async Task<ActionResult<ProjectRolesListViewModel>> Create([FromQuery] ProjectIdentification projectId, [FromBody] AddProjectRolesListViewModel model)
    {
        return Ok(await client.Create(projectId, model));
    }

    [HttpPost]
    [RequireMaster(Permission.CanEditRoles)]
    public async Task<ActionResult<ProjectRolesListViewModel>> Update([FromQuery] ProjectIdentification projectId, [FromBody] ProjectRolesList model)
    {
        if (model.ProjectRolesListId.ProjectId != projectId)
        {
            return BadRequest();
        }
        return Ok(await client.Update(model));
    }
}
