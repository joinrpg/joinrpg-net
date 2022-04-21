using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure;

/// <summary>
/// Sets ViewBag.IsProduction
/// </summary>
public class SetIsProductionFilterAttribute : ResultFilterAttribute
{
    /// <inheritedoc />
    public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is ViewResult viewResult)
        {
            var hostHost = context.HttpContext.Request.Host.Host;
            viewResult.ViewData["IsProduction"] = hostHost == "joinrpg.ru";
            viewResult.ViewData["FullHostName"] = context.HttpContext.Request.Scheme + hostHost;
        }
        return base.OnResultExecutionAsync(context, next);
    }
}
