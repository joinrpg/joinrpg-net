using System.Web.Mvc;
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
          return View(User);
        }

      public UserController(ApplicationUserManager userManager) : base(userManager)
      {
      }
    }
}