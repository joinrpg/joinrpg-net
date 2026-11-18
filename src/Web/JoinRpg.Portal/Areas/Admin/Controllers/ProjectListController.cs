using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class ProjectListController(
    ) : Portal.Controllers.Common.JoinMvcControllerBase
{
    public async Task<IActionResult> Index()
    {
        return View();
    }
}
