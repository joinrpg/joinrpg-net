using JoinRpg.Web.CharacterGroups.ProjectRoleGrid;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;

/// <summary>
/// Игроцкое отображение сетки ролей. Без [RequireMaster]: публичная сетка должна быть доступна
/// игроку/анониму. Реальная проверка (PublicMode + мастер) — в сервисе (ProjectRoleGridViewService),
/// который при отсутствии доступа возвращает результат с HasAccess == false (а не ошибку).
/// </summary>
[Route("/webapi/project-role-grid/[action]")]
public class ProjectRoleGridController(IProjectRoleGridClient client) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewResult>> Get(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int projectRolesListId)
    {
        var id = new ProjectRolesListIdentification(projectId, projectRolesListId);
        return Ok(await client.GetRoleGrid(id));
    }

    /// <summary>
    /// «Классическая» сетка ролей (GameGroups/Index) — строится на лету без сохранённой настройки.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewResult>> GetClassic(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int characterGroupId)
    {
        var groupId = new CharacterGroupIdentification(projectId, characterGroupId);
        return Ok(await client.GetClassicRoleGrid(groupId));
    }
}
