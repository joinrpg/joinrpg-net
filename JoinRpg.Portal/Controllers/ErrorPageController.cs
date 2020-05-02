using System.Diagnostics;
using JoinRpg.Portal.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using ControllerBase = JoinRpg.Portal.Controllers.Common.ControllerBase;

namespace JoinRpg.Portal.Controllers
{
    [AllowAnonymous]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class ErrorPageController : ControllerBase
    {
        [Route("/error/{statusCode?}")]
        public IActionResult Error(int statusCode)
        {
            var feature =
                HttpContext.Features.Get<IHttpRequestFeature>();


            return View(
                new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    Path = feature?.RawTarget,
                }
                );
        }
    }
}
