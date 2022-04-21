using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure.DiscoverFilters;

public class AddFullUriFilter : ActionFilterAttribute
{
    /// <inheritedoc />
    public override void OnActionExecuting(ActionExecutingContext context)
    {

        if (context.Controller is Controller controller)
        {
            controller.ViewBag.FullyQualifiedUri = controller.HttpContext.Request.GetEncodedUrl();
        }
        base.OnActionExecuting(context);
    }
}
