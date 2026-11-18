using JoinRpg.Portal.Infrastructure.Logging.Filters;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[ApiController]
[IgnoreAntiforgeryToken] //It's not required, because this is not browser-based API.
[TypeFilter<XApiRequestLoggingFilter>]
public class XGameApiController : ControllerBase
{
}
