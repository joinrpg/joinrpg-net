using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.Web.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AllowMasterHandler : AuthorizationHandler<MasterRequirement>
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

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
        {
            if (!context.User.Identity.IsAuthenticated)
            {
                context.Fail();
                return;
            }
            var projectIdAsObj = httpContextAccessor.HttpContext.Items[Constants.ProjectIdName];
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

            var userId = context.User.GetUserIdOrDefault();

            //Move this to claims to prevent DB call
            if (project.ProjectAcls.Any(acl => acl.UserId == userId && requirement.Permission.GetPermssionExpression()(acl)))
            {
                context.Succeed(requirement);
            }
        }
    }
}
