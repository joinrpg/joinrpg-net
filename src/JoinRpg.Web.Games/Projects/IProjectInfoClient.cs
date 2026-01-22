namespace JoinRpg.Web.Games.Projects;

public interface IProjectInfoClient
{
    Task<ProjectInfoViewModel> GetProjectInfo(ProjectIdentification projectId);
}
