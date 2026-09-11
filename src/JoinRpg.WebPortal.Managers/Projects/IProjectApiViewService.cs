using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Projects;

/// <summary>
/// Общий слой над проектами для x-game-api и (в будущем) MCP-инструментов (ADR012).
/// </summary>
public interface IProjectApiViewService
{
    Task<ProjectFieldsMetadata> GetFieldsMetadata(ProjectIdentification projectId);

    /// <summary>Поля, варианты и дерево групп проекта одним вызовом — точка входа для MCP (ADR012 §7).</summary>
    Task<ProjectOverview> GetOverview(ProjectIdentification projectId);

    Task<IEnumerable<ProjectHeader>> GetActiveProjects(UserIdentification userId);

    /// <summary>Проекты, где текущий пользователь — мастер (в отличие от <see cref="GetActiveProjects"/>, который включает и игровые заявки).</summary>
    Task<IEnumerable<ProjectHeader>> ListMasterProjects(UserIdentification userId);
}
