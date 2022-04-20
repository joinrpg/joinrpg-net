using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [Route("x-api/me")]
    public class MyProfileController : ControllerBase
    {
        private readonly IProjectRepository projectRepository;
        private readonly ICurrentUserAccessor currentUserAccessor;

        public MyProfileController(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor)
        {
            this.projectRepository = projectRepository;
            this.currentUserAccessor = currentUserAccessor;
        }

        /// <summary>
        /// All active projects that current user has access
        /// </summary>
        [HttpGet, Authorize, Route("projects/active")]
        public async Task<IEnumerable<ProjectHeader>> GetActiveProjects()
        {
            var userId = currentUserAccessor.UserId;
            return (await projectRepository.GetMyActiveProjectsAsync(userId)).Select(
                p => new ProjectHeader { ProjectId = p.ProjectId, ProjectName = p.ProjectName });
        }
    }
}
