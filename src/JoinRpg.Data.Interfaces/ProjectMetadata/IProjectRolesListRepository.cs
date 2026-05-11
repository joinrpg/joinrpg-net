namespace JoinRpg.Data.Interfaces.ProjectMetadata;

public interface IProjectRolesListRepository
{
    Task<IReadOnlyCollection<ProjectRolesList>> GetForProjectAsync(ProjectIdentification projectId);
    Task<ProjectRolesList?> GetByIdAsync(ProjectRolesListIdentification id);
}
