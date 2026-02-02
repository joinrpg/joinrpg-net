using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.Web.AdminTools;

public interface IProjectListForAdminClient
{
    Task<List<ProjectAdminListItemViewModel>> GetProjectsForAdmin(ProjectSelectionCriteria projectSelectionCriteria);
}
