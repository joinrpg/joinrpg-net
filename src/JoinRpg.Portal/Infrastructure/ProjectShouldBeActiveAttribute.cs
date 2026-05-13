using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.DiscoverFilters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace JoinRpg.Portal.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public class ProjectShouldBeActiveAttribute : TypeFilterAttribute
{
    public ProjectShouldBeActiveAttribute() : base(typeof(ProjectShouldBeActiveFilter))
    {
    }
}

public class ProjectShouldBeActiveFilter(IProjectMetadataRepository projectMetadataRepository) : IAsyncActionFilter, IAsyncPageFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (await ShouldBlockAsync(context.HttpContext))
        {
            context.Result = CreateErrorResult();
            return;
        }
        await next();
    }

    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {
        return Task.CompletedTask;
    }

    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {
        if (await ShouldBlockAsync(context.HttpContext))
        {
            context.Result = CreateErrorResult();
            return;
        }
        await next();
    }

    private async Task<bool> ShouldBlockAsync(HttpContext httpContext)
    {
        if (httpContext.TryGetProjectIdFromItems() is not ProjectIdentification projectId)
        {
            return false;
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        return !projectInfo.IsActive;
    }

    private static ViewResult CreateErrorResult()
    {
        return new ViewResult
        {
            ViewName = "ErrorNotActiveProject",
            ViewData = new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
        };
    }
}
