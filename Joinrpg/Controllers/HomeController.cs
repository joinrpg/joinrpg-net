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
    private const int projectsOnHomePage = 9;
    private readonly IProjectRepository _projectRepository;
    private readonly IClaimsRepository _claimsRepository;

    public HomeController(IProjectRepository projectRepository, ApplicationUserManager userManager, IClaimsRepository claimsRepository) : base (userManager)
    {
      _projectRepository = projectRepository;
      _claimsRepository = claimsRepository;
    }

    public async Task<ActionResult> Index()
    {
        return View(await LoadModel(projectsOnHomePage));
    }

    private async Task<HomeViewModel> LoadModel(int maxProjects = int.MaxValue)
    {
      

      var projects =
        (await _projectRepository.GetActiveProjectsWithClaimCount()).Select(p => new ProjectListItemViewModel()
        {
          ProjectId = p.ProjectId,
          IsMaster = p.HasMasterAccess(CurrentUserIdOrDefault),
          ProjectAnnounce = new MarkdownViewModel(p.Details?.ProjectAnnounce),
          ProjectName = p.ProjectName,
          MyClaims = p.Claims.Where(c => c.PlayerUserId == CurrentUserIdOrDefault),
          ClaimCount = p.Claims.Count(c => c.IsActive),
          IsAcceptingClaims = p.IsAcceptingClaims,
          ProjectRootGroupId = p.RootGroup.CharacterGroupId,
          IsRootGroupAccepting = p.RootGroup.IsAvailable
        }).Where(p => p.IsMaster || p.MyClaims.Any() || p.IsAcceptingClaims).ToList();
      var alwaysShowProjects =
        projects.Where(p => p.IsMaster || p.MyClaims.Any()).OrderByDescending(p => p.IsMaster).ThenByDescending(p => p.ClaimCount);
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

    public ActionResult About()
    {
      return View();
    }

    public ActionResult Contact()
    {
      ViewBag.Message = "Your contact page.";

      return View();
    }

    public ActionResult AboutTest()
    {
      return View();
    }

    public async Task<ViewResult> BrowseGames()
    {
      return View(await LoadModel());
    }
  }
}