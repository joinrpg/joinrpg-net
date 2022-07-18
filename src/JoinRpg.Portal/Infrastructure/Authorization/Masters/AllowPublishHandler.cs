using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Authorization;

namespace JoinRpg.Portal.Infrastructure.Authorization;

public class AllowPublishHandler : AuthorizationHandler<MasterRequirement>
{
    public AllowPublishHandler(
        IProjectRepository projectRepository,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AllowPublishHandler> logger)
    {
        ProjectRepository = projectRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger<AllowPublishHandler> logger;

    private IProjectRepository ProjectRepository { get; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (!requirement.AllowPublish)
        {
            return;
        }

        if (httpContextAccessor.HttpContext?.TryGetProjectIdFromItems() is not int projectId)
        {
            logger.LogError("Project id was not discovered, but master access required. That's probably problem with routing");
            return;
        }

        var project = await ProjectRepository.GetProjectAsync(projectId);

        if (project == null)
        {
            context.Fail();
            return;
        }

        if (project.Details.PublishPlot)
        {
            context.Succeed(requirement);
        }
    }
}
