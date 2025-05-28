namespace JoinRpg.Web.ProjectCommon.Projects;
public interface IProjectListClient
{
    Task<ProjectDto[]> GetProjects(ProjectSelectionCriteria projectSelectionCriteria);
}
