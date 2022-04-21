using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class AdminHomeController : JoinRpg.Portal.Controllers.Common.ControllerBase
{
    private IProjectRepository ProjectRepository { get; }


    public AdminHomeController(
        IProjectRepository projectRepository
    ) => ProjectRepository = projectRepository;

    public IActionResult Index() => View();

    public async Task<IActionResult> StaleGames()
    {
        var projects = await ProjectRepository.GetStaleProjects(DateTime.Now.AddMonths(-4));
        return View(projects);
    }
}
