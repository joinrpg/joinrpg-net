using System.Security.Claims;
using Joinrpg.Web.Identity;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization.Masters;

public class AllowMasterHandler(
    IHttpContextAccessor httpContextAccessor,
    ILogger<AllowMasterHandler> logger) : AuthorizationHandler<MasterRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (httpContextAccessor.HttpContext?.TryGetProjectIdFromItems() is not int projectId)
        {
            logger.LogError("Project id was not discovered, but master access required. That's probably problem with routing");
            return Task.CompletedTask;
        }

        if (context.User.FindFirstValue(PermissionEncoder.GetProjectPermissionClaimName(projectId)) is string projectClaim
            && PermissionEncoder.HasPermission(projectClaim, requirement.Permission))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
