using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class DumpConfigController : Controller
{
    private readonly IConfiguration configuration;

    public DumpConfigController(IConfiguration configuration)
        => this.configuration = configuration;

    public IActionResult Index()
    {
        var config = (configuration as IConfigurationRoot).GetDebugView();
        return Content(config);
    }
}
