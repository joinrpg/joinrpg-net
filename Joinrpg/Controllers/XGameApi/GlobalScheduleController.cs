using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.XGameApi.Contract;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [RoutePrefix("x-game-api/schedule")]
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
}
