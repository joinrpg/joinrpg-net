using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common
{
    public abstract class ControllerBase : Controller
    {

        protected ActionResult ViewIfFound(string viewName, object model)
            => model == null ? (ActionResult)NotFound() : View(viewName, model);

        protected ActionResult ViewIfFound(object model)
            => ViewIfFound(null, model);

        protected async Task<ActionResult> ViewIfFound<T>(Task<T> model)
            => ViewIfFound(null, await model);

        protected async Task<ActionResult> ViewIfFound<T>(string viewName, Task<T> model)
            => ViewIfFound(viewName, await model);
    }
}
