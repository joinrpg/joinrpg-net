using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : IProjectListClient
{
    public async Task<ProjectLinkViewModel[]> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        ProjectListSpecification spec = GetSpecification(projectSelectionCriteria);

        var projects = await projectRepository.GetProjectsBySpecification(currentUserAccessor.UserIdentification, spec);
        return [.. projects.Select(p => new ProjectLinkViewModel(new ProjectIdentification(p.ProjectId), p.ProjectName))];
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
