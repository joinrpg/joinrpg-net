using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

/// <summary>
/// Store projectId into ViewBag
/// </summary>
public class DiscoverProjectFilterAttribute : ActionFilterAttribute
{
    /// <inheritedoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {

        if (context.HttpContext.Items.TryGetValue(Constants.ProjectIdName, out var projectId) && context.Controller is Controller controller)
        {
            controller.ViewBag.ProjectId = projectId;
        }
        base.OnActionExecuting(context);
    }
}
