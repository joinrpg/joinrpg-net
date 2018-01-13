using System.Web.Mvc;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
    [AdminAuthorize]
    public class AdminHomeController : Web.Controllers.Common.ControllerBase
    {
        public ActionResult Index() => View();

        public AdminHomeController(ApplicationUserManager userManager) : base(userManager)
        {
        }
    }
}
