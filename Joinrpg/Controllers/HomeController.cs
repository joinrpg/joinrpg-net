using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Helpers;
using JoinRpg.Web.Models;

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

    public async Task<ActionResult> Index() => View(await LoadModel(false, ProjectsOnHomePage));

    private async Task<HomeViewModel> LoadModel(bool showInactive = false, int maxProjects = int.MaxValue)
    {
      var allProjects = showInactive
        ? await _projectRepository.GetArchivedProjectsWithClaimCount()
        : await _projectRepository.GetActiveProjectsWithClaimCount();

      var projects =
        allProjects
        .Select(p => new ProjectListItemViewModel(p, CurrentUserIdOrDefault))
        .Where(p => (showInactive && p.ClaimCount > 0) || p.IsMaster || p.MyClaims.Any(c => c.IsActive) || p.IsAcceptingClaims)
        .ToList();

      var alwaysShowProjects = ProjectListItemViewModel.OrderByDisplayPriority(
        projects.Where(p => p.IsMaster || p.MyClaims.Any()), p => p).ToList();

      var projectListItemViewModels = alwaysShowProjects.UnionUntilTotalCount(projects.OrderByDescending(p => p.ClaimCount), maxProjects);

      var finalProjects = projectListItemViewModels.ToList();

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

    public async Task<ActionResult> BrowseGames() => View(await LoadModel());

    public async Task<ActionResult> GameArchive() => View("BrowseGames", await LoadModel(true));
  }
}