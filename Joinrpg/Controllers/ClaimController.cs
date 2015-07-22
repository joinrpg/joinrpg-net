using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Controllers.Common;

namespace JoinRpg.Web.Controllers
{
  public class ClaimController : ControllerGameBase
  {
    public ActionResult Add(int projectid, int characterid)
    {
      throw new System.NotImplementedException();
    }

    public ClaimController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
    {
    }
  }
}