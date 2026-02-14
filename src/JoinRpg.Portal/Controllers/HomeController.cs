using JoinRpg.WebPortal.Managers.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers;

[AllowAnonymous]
public class HomeController(ProjectListManager projectListManager) : Common.JoinMvcControllerBase
{
    private const int ProjectsOnHomePage = 9;

    public async Task<ActionResult> Index() => View(await projectListManager.LoadHomeModel(ProjectsOnHomePage));

    public ActionResult About() => Redirect("/about");
    public ActionResult Funding2016() => Redirect("/about");
    public ActionResult HowToHelp() => Redirect("/about");
    public ActionResult FromAllrpgInfo() => Redirect("/about");

    public ActionResult Support() => View();

    public async Task<ActionResult> BrowseGames() => View(await projectListManager.LoadModel());

    public async Task<ActionResult> GameArchive() => View("BrowseGames", await projectListManager.LoadModel(true));
}
