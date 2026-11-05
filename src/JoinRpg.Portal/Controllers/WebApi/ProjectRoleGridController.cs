using JoinRpg.DomainTypes.Plots;
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
        [FromQuery] ProjectIdentification projectId, [FromQuery] ProjectRolesListIdentification rolesListId)
    {
        if (rolesListId is null || rolesListId.ProjectId != projectId)
        {
            return BadRequest();
        }
        return Ok(await client.GetRoleGrid(rolesListId));
    }

    /// <summary>Сетка, вшитая в вводную; доступ проверяет сервис по правилам ADR029.</summary>
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewResult>> GetForPlot(
        [FromQuery] ProjectIdentification projectId, [FromQuery] PlotElementIdentification plotElementId)
    {
        if (plotElementId is null || plotElementId.ProjectId != projectId)
        {
            return BadRequest();
        }
        return Ok(await client.GetRoleGridForPlot(plotElementId));
    }

    /// <summary>
    /// «Классическая» сетка ролей (GameGroups/Index) — строится на лету без сохранённой настройки.
    /// <paramref name="hotOnly"/> — сетка горячих ролей (GameGroups/Hot) вместо обычной классической.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ProjectRoleGridViewResult>> GetClassic(
        [FromQuery] ProjectIdentification projectId, [FromQuery] int? characterGroupId, [FromQuery] bool hotOnly = false)
    {
        var groupId = CharacterGroupIdentification.FromOptional(projectId, characterGroupId);
        return Ok(await client.GetClassicRoleGrid(projectId, groupId, hotOnly));
    }
}
