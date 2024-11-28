using JoinRpg.Data.Interfaces;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/schedule")]
public class GlobalScheduleController(IProjectRepository projectRepository) : XGameApiController
{
    /// <summary>
    /// All active projects with schedules
    /// </summary>
    [HttpGet, Authorize, Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
        return (await projectRepository.GetActiveProjectsWithSchedule())
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
