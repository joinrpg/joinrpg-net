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
            controller.ViewBag.IsProduction = context.HttpContext.Request.Host.Host == "joinrpg.ru";
            return base.OnResultExecutionAsync(context, next);
        }
    }
}
