using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/schedule")]
public class GlobalScheduleController(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : XGameApiController
{
    /// <summary>
    /// All active projects with schedules
    /// </summary>
    [HttpGet, Authorize, Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
        return (await projectRepository.GetProjectsBySpecification(currentUserAccessor.UserIdentification, ProjectListSpecification.ActiveProjectsWithSchedule))
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
