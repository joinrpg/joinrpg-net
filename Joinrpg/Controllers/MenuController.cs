using System.Linq;
using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models;

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
      var projects = ProjectRepository.GetMyActiveProjects(user).Select(p => new ProjectLinkViewModel()
      {
        ProjectId = p.ProjectId,
        ProjectName = p.ProjectName,
      });
      return PartialView(projects);
    }

    public MenuController(ApplicationUserManager userManager, IProjectRepository projectRepository,
      IProjectService projectService, IExportDataService exportDataService)
      : base(userManager, projectRepository, projectService, exportDataService)
    {
    }
  }
}