using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Read-only клиент игроцкого отображения сетки ролей.
/// Не путать с мастерским <c>IProjectRolesListClient</c> (управление) в ProjectMasterTools.
/// </summary>
public interface IProjectRoleGridClient
{
    Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id);

    /// <summary>
    /// «Классическая» сетка ролей (страница GameGroups/Index) — строится на лету, без сохранённой
    /// настройки. <paramref name="groupId"/> — явно выбранный корень сетки, или null, если корень
    /// не указан в URL: тогда сетка строится от корня проекта, не используя спецгруппы.
    /// </summary>
    Task<ProjectRoleGridViewResult> GetClassicRoleGrid(ProjectIdentification projectId, CharacterGroupIdentification? groupId);
}
