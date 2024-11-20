using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class AdminHomeController(IProjectRepository projectRepository) : Controller
{
    public IActionResult Index() => View();

    public async Task<IActionResult> StaleGames()
    {
        var stale = await projectRepository.GetStaleProjects(DateTime.Now.AddMonths(-4));
        return View(stale);
    }
}
