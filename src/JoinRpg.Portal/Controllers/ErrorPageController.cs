using System.Diagnostics;
using JoinRpg.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ControllerBase = JoinRpg.Portal.Controllers.Common.ControllerBase;

namespace JoinRpg.Portal.Controllers
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorPageController : ControllerBase
    {
        private readonly ILogger<ErrorPageController> logger;

        public ErrorPageController(ILogger<ErrorPageController> logger)
        {
            this.logger = logger;
        }

        [Route("/error/404")]
        public IActionResult NotFound(int statusCode)
        {
            return View();
        }

        [Route("/error/{statusCode?}")]
        public IActionResult Error(int statusCode)
        {
            var feature =
                HttpContext.Features.Get<IHttpRequestFeature>();
            var exceptionHandlerPathFeature =
                HttpContext.Features.Get<IExceptionHandlerPathFeature>();

            if (exceptionHandlerPathFeature is not null)
            {
                logger.LogError(exceptionHandlerPathFeature.Error,
                    "Exception during web request in {errorPath}",
                    exceptionHandlerPathFeature.Path);
            }
            else
            {
                logger.LogError("It's suspicios that we hit error handler w/o exception");
            }

            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? "",
                    AspNetTrace = HttpContext.TraceIdentifier,
                    Path = feature?.RawTarget ?? "NO PATH",
                }
                );
        }
    }
}
