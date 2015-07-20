using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Dal.Impl;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Web.Models;

namespace JoinRpg.Web.Controllers
{
  public class HomeController : ControllerBase
  {
    private readonly IProjectRepository _projectRepository;
    private readonly IUserRepository _userRepository;

    public HomeController(IProjectRepository projectRepository, ApplicationUserManager userManager, IUserRepository userRepository) : base (userManager)
    {
      _projectRepository = projectRepository;
      _userRepository = userRepository;
    }

    public ActionResult Index()
    {
        return View(LoadModel());
    }

    private HomeViewModel LoadModel()
    {

      return new HomeViewModel()
      {
        ActiveProjects = _projectRepository.ActiveProjects,
        MyProjects =
          User.Identity.IsAuthenticated
            ? _projectRepository.GetMyActiveProjects(CurrentUserId)
            : new Project[] {}
      };
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
  }
}