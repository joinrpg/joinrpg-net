using JoinRpg.Data.Interfaces;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi;

[Route("x-game-api/schedule")]
public class GlobalScheduleController : XGameApiController
{
    public GlobalScheduleController(IProjectRepository projectRepository) : base(projectRepository)
    {
    }

    /// <summary>
    /// All active projects with schedules
    /// </summary>
    [HttpGet, Authorize, Route("projects/active")]
    public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
    {
        return (await ProjectRepository.GetActiveProjectsWithSchedule())
            .Select(p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
    }
}
