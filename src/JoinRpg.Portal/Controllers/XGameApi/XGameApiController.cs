using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[ApiController]
[IgnoreAntiforgeryToken] //It's not required, because this is not browser-based API.
public class XGameApiController : ControllerBase
{
}
