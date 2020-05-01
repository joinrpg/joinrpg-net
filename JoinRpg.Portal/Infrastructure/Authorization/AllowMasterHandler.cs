using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Filter;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AllowMasterHandler : AuthorizationHandler<AllowMasterRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly ILogger<AllowMasterHandler> logger;

        public AllowMasterHandler(
            IProjectRepository projectRepository,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AllowMasterHandler> logger)
        {
            ProjectRepository = projectRepository;
            this.httpContextAccessor = httpContextAccessor;
            this.logger = logger;
        }
        private IProjectRepository ProjectRepository { get; }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowMasterRequirement requirement)
        {

            var projectIdAsObj = httpContextAccessor.HttpContext.Items["ProjectId"];
            if (projectIdAsObj == null || !int.TryParse(projectIdAsObj.ToString(), out var projectId))
            {
                logger.LogError("Project id was not discovered, but master access required. That's probably problem with routing");
                context.Fail();
                return;
            }
            var project = await ProjectRepository.GetProjectAsync(projectId);

            if (project == null)
            {
                logger.LogInformation("Failed to load Project={projectId}, that's incorrect id. Should be accompanied by 404.", projectId);
                context.Fail();
                return;
            }

            //Move this to claims to prevent DB call
            if (project.ProjectAcls.Any(acl => acl.UserId.ToString() == context.User.Identity.GetUserId() && requirement.Permission.GetPermssionExpression()(acl)))
            {
                context.Succeed(requirement);
            }
        }
    }
}
