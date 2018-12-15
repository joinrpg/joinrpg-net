using System.Web.Mvc;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class AdminHomeController : Web.Controllers.Common.ControllerBase
    {
        public ActionResult Index() => View();

        public AdminHomeController(ApplicationUserManager userManager,
            IUserRepository userRepository) : base(userManager, userRepository)
        {
        }
    }
}
