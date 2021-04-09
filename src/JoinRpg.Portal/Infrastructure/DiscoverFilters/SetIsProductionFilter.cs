using System.Linq;
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
            if (context.Result is ViewResult viewResult)
            {
                // Request.Host contains URL with port "beta.example.com:5001"
                // Request.Host.Host contains URL without port "beta.example.com"
                var hostHost = context.HttpContext.Request.Host.Host;
                // For check, we should take top domain word
                var topDomain = hostHost.Split('.').First();
                // And make it Dev if starts with "www" or subdomain (i.e. "dev")
                viewResult.ViewData["IsProduction"] = topDomain == "joinrpg";
                viewResult.ViewData["FullHostName"] = context.HttpContext.Request.Scheme + hostHost;
            }
            return base.OnResultExecutionAsync(context, next);
        }
    }
}
