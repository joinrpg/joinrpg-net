namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectListClient
{
    Task<ProjectLinkViewModel[]> GetProjects(ProjectSelectionCriteria projectSelectionCriteria);
}
