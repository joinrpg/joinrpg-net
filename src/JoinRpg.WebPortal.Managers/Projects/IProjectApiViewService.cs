using JoinRpg.XGameApi.Contract;

namespace JoinRpg.WebPortal.Managers.Projects;

/// <summary>
/// Общий слой над проектами для x-game-api и (в будущем) MCP-инструментов (ADR012).
/// </summary>
public interface IProjectApiViewService
{
    Task<ProjectFieldsMetadata> GetFieldsMetadata(ProjectIdentification projectId);

    Task<IEnumerable<ProjectHeader>> GetActiveProjects(UserIdentification userId);
}
