using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.ProjectCommon.Projects;

namespace JoinRpg.WebPortal.Managers.Projects;
internal class ProjectListViewService(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : IProjectListClient
{
    public async Task<ProjectDto[]> GetProjectsWithMyMasterAccess()
    {
        var projects = await projectRepository.GetMyActiveProjectsAsync(currentUserAccessor.UserId);
        return [.. projects.Select(p => new ProjectDto(new PrimitiveTypes.ProjectIdentification(p.ProjectId), p.ProjectName))];
    }
}
