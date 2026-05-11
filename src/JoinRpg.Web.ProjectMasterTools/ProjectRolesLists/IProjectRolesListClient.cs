namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public interface IProjectRolesListClient
{
    Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId);
    Task Remove(ProjectRolesListIdentification id);
    Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model);
    Task<ProjectRolesListViewModel> Update(DomainTypes.ProjectMetadata.ProjectRolesList model);
}
