using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : IProjectListClient
{
    public async Task<ProjectDto[]> GetProjects(ProjectSelectionCriteria projectSelectionCriteria)
    {
        var spec = projectSelectionCriteria switch
        {
            ProjectSelectionCriteria.ForCloning => ProjectListSpecification.ForCloning,
            ProjectSelectionCriteria.ActiveWithMyMasterAccess => ProjectListSpecification.ActiveWithMyMasterAccess,
            _ => throw new NotImplementedException()
        };

        var projects = await projectRepository.GetProjectsBySpecification(currentUserAccessor.UserIdentification, spec);
        return [.. projects.Select(p => new ProjectDto(new ProjectIdentification(p.ProjectId), p.ProjectName))];
    }
}
