using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Areas.Admin.Models;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class AdminHomeController(IProjectRepository projectRepository) : Controller
{
    public async Task<IActionResult> StaleGames()
    {
        var stale = await projectRepository.GetStaleProjects(DateTime.Now.AddMonths(-4));
        var blocked = await projectRepository.GetBlockedProjects();
        return View(new StaleGamesViewModel(stale, blocked));
    }
}
