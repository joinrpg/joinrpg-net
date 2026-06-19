using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Игроцкое отображение сетки ролей. Без [RequireMaster]: публичная сетка должна быть доступна
/// игроку/анониму. Реальная проверка (PublicMode + мастер) — в сервисе (ProjectRoleGridViewService).
/// </summary>
[Route("/webapi/project-role-grid/[action]")]
public class ProjectRoleGridController(IProjectRoleGridClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewModel>> Get(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int projectRolesListId)
    {
        var id = new ProjectRolesListIdentification(projectId, projectRolesListId);
        return Ok(await client.GetRoleGrid(id));
    }
}
