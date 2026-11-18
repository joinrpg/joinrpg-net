using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

/// <summary>
/// Store projectId into ViewBag
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class DiscoverProjectPageFilterAttribute : Attribute, IPageFilter
{
    public void OnPageHandlerExecuted(PageHandlerExecutedContext context) { }
    public void OnPageHandlerExecuting(PageHandlerExecutingContext context)
    {
        if (context.HttpContext.Items.TryGetValue(Constants.ProjectIdName, out var projectId) && context.HandlerInstance is PageModel page)
        {
            page.ViewData["ProjectId"] = projectId;
        }
    }
    public void OnPageHandlerSelected(PageHandlerSelectedContext context) { }
}
