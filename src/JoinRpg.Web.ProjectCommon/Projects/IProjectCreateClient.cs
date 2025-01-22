namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectCreateClient
{
    Task<ProjectIdentification> CreateProject(ProjectCreateViewModel model);
}
