using JetBrains.Annotations;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.Common
{
    public abstract class ControllerBase : Controller
    {

        protected ActionResult ViewIfFound([AspMvcView] string? viewName, object? model)
            => model == null ? (ActionResult)NotFound() : View(viewName, model);

        [AspMvcView]
        protected ActionResult ViewIfFound(object? model)
            => ViewIfFound(null, model);

        [AspMvcView]
        protected async Task<ActionResult> ViewIfFound<T>(Task<T> model)
            => ViewIfFound(null, await model);

        protected async Task<ActionResult> ViewIfFound<T>([AspMvcView] string viewName, Task<T> model)
            => ViewIfFound(viewName, await model);


        protected ActionResult NotModified() => new StatusCodeResult(StatusCodes.Status304NotModified);
    }
}
