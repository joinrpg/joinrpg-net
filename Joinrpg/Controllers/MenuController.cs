using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Controllers
{
  public class MenuController : Common.ControllerGameBase
  {
    //TODO: Current ASP.net MVC doesn't support async child actions. This limitations will be lifted in ASP.vnext
    public ActionResult MyProjectDropdown()
    {
      var user = CurrentUserIdOrDefault;
      if (user == null)
      {
        return new EmptyResult();
      }
      var projects = ProjectRepository.GetMyActiveProjects(user);
      return PartialView(projects);
    }

    public MenuController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }
  }
}