using System.Linq;
using System.Web.Mvc;
using JoinRpg.Web.Models;
using Microsoft.AspNet.Identity;

namespace JoinRpg.Web.Controllers
{
    public class UserController : ControllerBase
    {

        [HttpGet]
        // GET: User
        public ActionResult Details(string email)
        {
          var user = UserManager.FindByEmail(email);
          return View(new UserProfileViewModel()
          {
            DisplayName = user.DisplayName,
            FullName = user.FullName,
            ThisUserProjects = user.ProjectAcls,
            CanGrantAccessProjects = LoadForCurrentUser(userId =>  GetCurrentUser().ProjectAcls.Where(acl => acl.CanGrantRights).Select(acl => acl.Project)),
            UserId = user.UserId
          });
        }

      public UserController(ApplicationUserManager userManager) : base(userManager)
      {
      }
    }
}