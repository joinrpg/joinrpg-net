using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Filter;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.Authorization
{
    public class AllowMasterHandler : AuthorizationHandler<AllowMasterRequirement>
    {
        public AllowMasterHandler(IProjectRepository projectRepository)
        {
            ProjectRepository = projectRepository;
        }
        private IProjectRepository ProjectRepository { get; }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AllowMasterRequirement requirement)
        {
            if (!(context.Resource is AuthorizationFilterContext mvcContext))
            {
                return;
            }
            var projectIdAsObj = mvcContext.HttpContext.Items["ProjectId"];
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
            if (project.ProjectAcls.Any(acl => acl.UserId.ToString() == context.User.Identity.Name && requirement.Permission.GetPermssionExpression()(acl)))
            {
                context.Succeed(requirement);
            }
        }
    }
}
