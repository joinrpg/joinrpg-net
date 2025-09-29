using JoinRpg.WebPortal.Managers.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[AllowAnonymous]
public class HomeController : Common.ControllerBase
{
    private ProjectListManager ProjectListManager { get; }
    private const int ProjectsOnHomePage = 9;

    public HomeController(ProjectListManager projectListManager) => ProjectListManager = projectListManager;

    public async Task<ActionResult> Index() =>
        View(await ProjectListManager.LoadModel(false, ProjectsOnHomePage));


    public ActionResult About() => RedirectToPage("/about");

    public ActionResult Funding2016() => RedirectToPage("/about");

    public ActionResult HowToHelp() => RedirectToPage("/about");

    public ActionResult Support() => View();

    public ActionResult FromAllrpgInfo() => RedirectToPage("/about");

    public async Task<ActionResult> BrowseGames() => View(await ProjectListManager.LoadModel());

    public async Task<ActionResult> GameArchive() =>
        View("BrowseGames", await ProjectListManager.LoadModel(true));
}
