using JoinRpg.Data.Interfaces;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class ProjectListViewService(IProjectRepository projectRepository) : IProjectListClient, IProjectListForAdminClient
{
    public async Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        ProjectListSpecification spec = GetSpecification(projectSelectionCriteria);

        var projects = await projectRepository.GetProjectsBySpecification(spec);
        return [.. projects.Select(p => new ProjectLinkViewModel(p.ProjectId, p.ProjectName))];
    }

    async Task<List<ProjectAdminListItemViewModel>> IProjectListForAdminClient.GetProjectsForAdmin(ProjectSelectionCriteria projectSelectionCriteria)
    {
        ProjectListSpecification spec = GetSpecification(projectSelectionCriteria);

        var projects = await projectRepository.GetProjectsBySpecification(spec);
        return [.. projects.Select(p => new ProjectAdminListItemViewModel(
            p.ProjectId,
            p.ProjectName,
            p.LastUpdatedAt,
            KiLinks: null
            ))];
    }

    internal static ProjectListSpecification GetSpecification(ProjectSelectionCriteria projectSelectionCriteria)
    {
        return projectSelectionCriteria switch
        {
            ProjectSelectionCriteria.ForCloning => ProjectListSpecification.ForCloning,
            ProjectSelectionCriteria.ActiveWithMyMasterAccess => ProjectListSpecification.ActiveWithMyMasterAccess,
            ProjectSelectionCriteria.ActiveWithoutKogdaIgra => ProjectListSpecification.ActiveProjectsWithoutKogdaIgra,
            _ => throw new NotImplementedException()
        };
    }
}
