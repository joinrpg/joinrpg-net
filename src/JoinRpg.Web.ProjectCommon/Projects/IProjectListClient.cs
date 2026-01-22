namespace JoinRpg.Web.ProjectCommon.Projects;

public interface IProjectListClient
{
    Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria projectSelectionCriteria);
}
