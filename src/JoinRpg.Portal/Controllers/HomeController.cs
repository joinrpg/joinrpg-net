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


    public ActionResult About() => Redirect("/about");

    public ActionResult Funding2016() => Redirect("/about");

    public ActionResult HowToHelp() => Redirect("/about");

    public ActionResult Support() => View();

    public ActionResult FromAllrpgInfo() => Redirect("/about");

    public async Task<ActionResult> BrowseGames() => View(await ProjectListManager.LoadModel());

    public async Task<ActionResult> GameArchive() =>
        View("BrowseGames", await ProjectListManager.LoadModel(true));
}
