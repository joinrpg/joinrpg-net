using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-api/me")]
public class MyProfileController(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : XGameApiController
{

    /// <summary>
    /// All active projects that current user has access
    /// </summary>
    [HttpGet, Authorize, Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
        return (await projectRepository.GetPersonalizedProjectsBySpecification(ProjectListSpecification.MyActiveProjects(currentUserAccessor.UserIdentification)))
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
