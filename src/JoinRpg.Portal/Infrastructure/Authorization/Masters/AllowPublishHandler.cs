using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.Authorization;

public class AllowPublishHandler : AuthorizationHandler<MasterRequirement>
{
    public AllowPublishHandler(IProjectRepository projectRepository) => ProjectRepository = projectRepository;
    private IProjectRepository ProjectRepository { get; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, MasterRequirement requirement)
    {
        if (!requirement.AllowPublish)
        {
            return;
        }
        if (!(context.Resource is AuthorizationFilterContext mvcContext))
        {
            return;
        }
        var projectIdAsObj = mvcContext.HttpContext.Items[Constants.ProjectIdName];
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

        if (project.Details.PublishPlot)
        {
            context.Succeed(requirement);
        }
    }
}
