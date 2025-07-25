namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectInfoClient
{
    Task<ProjectInfoViewModel> GetProjectInfo(ProjectIdentification projectId);
}
