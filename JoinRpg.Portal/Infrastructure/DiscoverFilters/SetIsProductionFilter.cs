using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure
{
    /// <summary>
    /// Sets ViewBag.IsProduction
    /// </summary>
    public class SetIsProductionFilterAttribute : ResultFilterAttribute
    {
        /// <inheritedoc />
        public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var controller = context.Controller as Controller;
            var hostHost = context.HttpContext.Request.Host.Host;
            controller.ViewBag.IsProduction = hostHost == "joinrpg.ru";
            controller.ViewBag.FullHostName = context.HttpContext.Request.Scheme + hostHost;
            return base.OnResultExecutionAsync(context, next);
        }
    }
}
