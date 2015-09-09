using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Web.Controllers
{
    public class CharacterController : Common.ControllerGameBase
    {
        // GET: Character
      public CharacterController(ApplicationUserManager userManager, IProjectRepository projectRepository, IProjectService projectService) : base(userManager, projectRepository, projectService)
      {
      }

    public ActionResult Details(int projectid, int characterid)
    {
      return WithCharacter(projectid, characterid, (project, character) => View(character));
    }
  }
}