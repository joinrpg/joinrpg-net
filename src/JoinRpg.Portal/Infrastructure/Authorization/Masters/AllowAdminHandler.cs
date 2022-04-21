using JoinRpg.DataModel;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization;

public class AllowAdminHandler : AuthorizationHandler<MasterRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (requirement.AllowAdmin && context.User.IsInRole(Security.AdminRoleName))
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;

    }
}
