namespace JoinRpg.Services.Interfaces.ProjectMetadata;

public interface IProjectRolesListService
{
    Task<ProjectRolesList> CreateAsync(ProjectRolesList model);
    Task<ProjectRolesList> UpdateAsync(ProjectRolesList model);
    Task RemoveAsync(ProjectRolesListIdentification id);
    Task<ProjectRolesList> GetByIdAsync(ProjectRolesListIdentification id);
}
