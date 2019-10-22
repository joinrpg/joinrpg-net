using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [Route("x-api/me")]
    public class MyProfileController : XGameApiController
    {
        public MyProfileController(IProjectRepository projectRepository) : base(projectRepository)
        {
        }

        /// <summary>
        /// All active projects that current user has access
        /// </summary>
        [HttpGet, Authorize, Route("projects/active")]
        public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
        {
            return (await ProjectRepository.GetMyActiveProjectsAsync(GetCurrentUserId())).Select(
                p => new ProjectHeader {ProjectId = p.ProjectId, ProjectName = p.ProjectName});
        }
    }
}
