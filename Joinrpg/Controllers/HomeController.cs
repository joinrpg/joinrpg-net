using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Web.Models;
using JoinRpg.Web.Models.CommonTypes;

namespace JoinRpg.Web.Controllers
{
  public class HomeController : Common.ControllerBase
  {
    private const int ProjectsOnHomePage = 9;
    private readonly IProjectRepository _projectRepository;

    public HomeController(IProjectRepository projectRepository, ApplicationUserManager userManager) : base (userManager)
    {
      _projectRepository = projectRepository;
    }

    public async Task<ActionResult> Index() => View(await LoadModel(ProjectsOnHomePage));

    private async Task<HomeViewModel> LoadModel(int maxProjects = int.MaxValue)
    {
      var projects =
        (await _projectRepository.GetActiveProjectsWithClaimCount())
        .Select(p => ProjectListItemViewModel.FromProject(p, CurrentUserIdOrDefault))
        .Where(p => p.IsMaster || p.MyClaims.Any() || p.IsAcceptingClaims)
        .ToList();

      var alwaysShowProjects = ProjectListItemViewModel.OrderByDisplayPriority(
        projects.Where(p => p.IsMaster || p.MyClaims.Any()), p => p);
      var otherProjects =
        projects.Except(alwaysShowProjects)
          .OrderByDescending(p => p.ClaimCount)
          .Take(Math.Max(0, maxProjects - alwaysShowProjects.Count())); // Add more projects until we have 9 total

      var finalProjects = alwaysShowProjects.Union(otherProjects).ToList();

      return new HomeViewModel
      {
        ActiveProjects = finalProjects,
        HasMoreProjects = projects.Count > finalProjects.Count
      };
    }

    public ActionResult About() => View();

    public ActionResult Contact()
    {
      ViewBag.Message = "Your contact page.";

      return View();
    }

    public ActionResult HowToHelp() => View();

    public ActionResult FromAllrpgInfo() => View();

    public async Task<ViewResult> BrowseGames()
    {
      return View(await LoadModel());
    }
  }
}