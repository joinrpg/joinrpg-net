using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization;

public class AllowPublishHandler(
    IProjectMetadataRepository projectRepository,
    IHttpContextAccessor httpContextAccessor,
    ILogger<AllowPublishHandler> logger) : AuthorizationHandler<MasterRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (!requirement.AllowPublish)
        {
            return;
        }

        if (httpContextAccessor.HttpContext?.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            logger.LogError("Project id was not discovered, but master access required. That's probably problem with routing");
            return;
        }

        var project = await projectRepository.GetProjectMetadata(projectId);

        if (project.PublishPlot)
        {
            context.Succeed(requirement);
        }
    }
}
