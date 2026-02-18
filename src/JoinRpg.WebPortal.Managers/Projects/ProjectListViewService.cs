using JoinRpg.Common.KogdaIgraClient;
using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.AdminTools;
using JoinRpg.Web.ProjectCommon.Projects;
using JoinRpg.WebPortal.Managers.AdminTools;

namespace JoinRpg.WebPortal.Managers.Projects;

internal class ProjectListViewService(
    IProjectRepository projectRepository,
    IOptions<KogdaIgraOptions> kograIgraOptions,
    ICurrentUserAccessor currentUserAccessor
    ) : IProjectListClient, IProjectListForAdminClient
{
    public async Task<List<ProjectLinkViewModel>> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        ProjectListSpecification spec = GetSpecification(currentUserAccessor.UserIdentificationOrDefault, projectSelectionCriteria);

        var projects = await projectRepository.GetProjectsBySpecification(spec);
        return [.. projects.Select(p => new ProjectLinkViewModel(p.ProjectId, p.ProjectName))];
    }

    async Task<List<ProjectAdminListItemViewModel>> IProjectListForAdminClient.GetProjectsForAdmin(ProjectSelectionCriteria projectSelectionCriteria)
    {
        ProjectListSpecification spec = GetSpecification(currentUserAccessor.UserIdentificationOrDefault, projectSelectionCriteria);

        var projects = await projectRepository.GetProjectsBySpecification(spec);
        return [.. projects.Select(p => new ProjectAdminListItemViewModel(
            p.ProjectId,
            p.ProjectName,
            p.LastUpdatedAt,
            KiLinks: p.KiLinks?.Select(x => x.ToViewModel(kograIgraOptions.Value)).ToList()
            ))];
    }

    internal static ProjectListSpecification GetSpecification(UserIdentification? userId, ProjectSelectionCriteria projectSelectionCriteria)
    {
        return projectSelectionCriteria switch
        {
            ProjectSelectionCriteria.ForCloning when userId is not null => ProjectListSpecification.ForCloning(userId),
            ProjectSelectionCriteria.ActiveWithMyMasterAccess when userId is not null => ProjectListSpecification.ActiveWithMyMasterAccess(userId),
            ProjectSelectionCriteria.ActiveWithoutKogdaIgra => ProjectListSpecification.ActiveProjectsWithoutKogdaIgra,
            ProjectSelectionCriteria.All => ProjectListSpecification.All,
            ProjectSelectionCriteria.Active => ProjectListSpecification.Active,
            _ => throw new NotImplementedException()
        };
    }
}
