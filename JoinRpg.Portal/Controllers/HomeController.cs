using System;
using System.Threading.Tasks;
using JoinRpg.WebPortal.Managers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers
{
    [AllowAnonymous]
    public class HomeController : Common.ControllerBase
    {
        private ProjectListManager ProjectListManager { get; }
        private const int ProjectsOnHomePage = 9;

        public HomeController(ProjectListManager projectListManager) => ProjectListManager = projectListManager;

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> Index(string culture)
        {
            if (!string.IsNullOrEmpty(culture))
            {
                Response.Cookies.Append(
                    CookieRequestCultureProvider.DefaultCookieName,
                    CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
                );
            }

            return View(await ProjectListManager.LoadModel(false, ProjectsOnHomePage));
        }


        [AllowAnonymous]
        [HttpGet]
        public ActionResult About() => View();

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Funding2016() => View();

        [AllowAnonymous]
        [HttpGet]
        public ActionResult HowToHelp() => View();

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Support() => View();

        [AllowAnonymous]
        [HttpGet]
        public ActionResult FromAllrpgInfo() => View();

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> BrowseGames() => View(await ProjectListManager.LoadModel());

        [AllowAnonymous]
        [HttpGet]
        public async Task<ActionResult> GameArchive() =>
            View("BrowseGames", await ProjectListManager.LoadModel(true));
    }
}
