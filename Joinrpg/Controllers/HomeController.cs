using System;
using System.Collections.Generic;
using System.Linq;
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
    private readonly IClaimsRepository _claimsRepository;

    public HomeController(IProjectRepository projectRepository, ApplicationUserManager userManager, IClaimsRepository claimsRepository) : base (userManager)
    {
      _projectRepository = projectRepository;
      _claimsRepository = claimsRepository;
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
          LoadForUser(userId => _projectRepository.GetMyActiveProjects(userId)),
        MyClaims = LoadForUser(userId => _claimsRepository.GetClaimsForUser(userId).Where(claim => claim.IsActive))
      };
    }

    private IEnumerable<T> LoadForUser<T>(Func<int, IEnumerable<T>> loadFunc)
    {
      return User.Identity.IsAuthenticated
        ? loadFunc(CurrentUserId)
        : new T[] {};
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