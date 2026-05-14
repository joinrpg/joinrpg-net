using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ProjectShouldBeActiveAttribute : ActionFilterAttribute, IAsyncPageFilter
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        await CheckProjectActiveAsync(context.HttpContext);
        await next();
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        await CheckProjectActiveAsync(context.HttpContext);
        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context) => Task.CompletedTask;

    private static async Task CheckProjectActiveAsync(HttpContext httpContext)
    {
        if (httpContext.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            return;
        }

        var projectMetadataRepository = httpContext.RequestServices.GetRequiredService<IProjectMetadataRepository>();
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.IsActive)
        {
            throw new ProjectDeactivatedException(projectId);
        }
    }
}
