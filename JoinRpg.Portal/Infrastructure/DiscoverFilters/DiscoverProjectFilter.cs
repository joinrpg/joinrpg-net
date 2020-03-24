using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace JoinRpg.Portal.Infrastructure
{
    /// <summary>
    /// Store projectId for CurrentProjectAccessor
    /// </summary>
    public class DiscoverProjectFilterAttribute : ActionFilterAttribute
    {
        public const string ProjectIdName = "ProjectId";

        /// <inheritedoc />
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContextItems = context.HttpContext.Items;

            if (context.ActionArguments.TryGetValue(ProjectIdName, out var projectId))
            {
                httpContextItems[ProjectIdName] = projectId;
                if (context.Controller is Controller controller)
                {
                    controller.ViewBag.ProjectId = projectId;
                }
            }
            base.OnActionExecuting(context);
        }
    }
}
