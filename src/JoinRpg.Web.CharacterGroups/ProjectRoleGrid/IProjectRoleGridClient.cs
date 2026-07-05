using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.Web.CharacterGroups.ProjectRoleGrid;

/// <summary>
/// Read-only клиент игроцкого отображения сетки ролей.
/// Не путать с мастерским <c>IProjectRolesListClient</c> (управление) в ProjectMasterTools.
/// </summary>
public interface IProjectRoleGridClient
{
    Task<ProjectRoleGridViewResult> GetRoleGrid(ProjectRolesListIdentification id);
}
