namespace JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;

public interface IProjectRolesListClient
{
    Task<ProjectRolesListViewModel> GetList(ProjectIdentification projectId);
    Task<ProjectRolesList> GetById(ProjectRolesListIdentification id);
    Task Remove(ProjectRolesListIdentification id);
    Task<ProjectRolesListViewModel> Create(ProjectIdentification projectId, AddProjectRolesListViewModel model);
    Task<ProjectRolesListViewModel> Update(ProjectRolesList model);
}
