using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authentication;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization;

public class AllowMasterHandler(
    IProjectMetadataRepository projectMetadataRepository,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AllowMasterHandler> logger) : AuthorizationHandler<MasterRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            // Do not need to return fail - if nobody will mark requirement as success, will fail
            return;
        }

        if (httpContextAccessor.HttpContext?.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            logger.LogError("Project id was not discovered, but master access required. That's probably problem with routing");
            return;
        }

        var project = await projectMetadataRepository.GetProjectMetadata(projectId);

        var userId = context.User.GetUserIdOrDefault();

        if (userId is not null && project.HasMasterAccess(new UserIdentification(userId.Value), requirement.Permission))
        {
            context.Succeed(requirement);
        }
    }
}
