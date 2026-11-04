namespace JoinRpg.Web.ProjectCommon;

/// <summary>
/// Минимум клиента сеток ролей, который нужен <see cref="RolesListSelector"/>. Живёт рядом
/// с селектором в ProjectCommon, чтобы потребителям (например, Web.Plots) не тянуть зависимость
/// от всего клиента сеток ролей из Web.ProjectMasterTools.
/// </summary>
public interface IRolesListSelectorClient
{
    /// <summary>Сохранённые сетки ролей проекта.</summary>
    Task<ProjectRolesListDto[]> GetRolesListsList(ProjectIdentification projectId);
}
