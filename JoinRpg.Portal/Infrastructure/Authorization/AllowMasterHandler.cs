using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Filter;
using Microsoft.AspNet.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AllowMasterHandler : AuthorizationHandler<AllowMasterRequirement>
    {
        private readonly IHttpContextAccessor httpContextAccessor;

        public AllowMasterHandler(IProjectRepository projectRepository, IHttpContextAccessor httpContextAccessor)
        {
            ProjectRepository = projectRepository;
            this.httpContextAccessor = httpContextAccessor;
        }
        private IProjectRepository ProjectRepository { get; }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowMasterRequirement requirement)
        {

            var projectIdAsObj = httpContextAccessor.HttpContext.Items["ProjectId"];
            if (projectIdAsObj == null || !int.TryParse(projectIdAsObj.ToString(), out var projectId))
            {
                context.Fail();
                return;
            }
            var project = await ProjectRepository.GetProjectAsync(projectId);

            if (project == null)
            {
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
