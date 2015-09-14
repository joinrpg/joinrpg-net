using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class HomeController : Common.ControllerBase
  {
    private readonly IProjectRepository _projectRepository;
    private readonly IClaimsRepository _claimsRepository;

    public HomeController(IProjectRepository projectRepository, ApplicationUserManager userManager, IClaimsRepository claimsRepository) : base (userManager)
    {
      _projectRepository = projectRepository;
      _claimsRepository = claimsRepository;
    }

    public async Task<ActionResult> Index()
    {
        return View(await LoadModel());
    }

    private async Task<HomeViewModel> LoadModel()
    {
      var homeViewModel = new HomeViewModel();
      
      if (User.Identity.IsAuthenticated)
      {
        homeViewModel.MyClaims = await _claimsRepository.GetActiveClaimsForUser(CurrentUserId);
        homeViewModel.MyProjects = await _projectRepository.GetMyActiveProjectsAsync(CurrentUserId);
      }
      homeViewModel.ActiveProjects = await _projectRepository.GetActiveProjects();
      return homeViewModel;
    }

    public ActionResult About()
    {
      ViewBag.Message = "Your application description page.";

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
  }
}