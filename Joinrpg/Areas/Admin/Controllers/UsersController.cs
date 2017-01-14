using System.Threading.Tasks;
using System.Web.Mvc;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Areas.Admin.Models;
using JoinRpg.Web.Helpers;

namespace JoinRpg.Web.Areas.Admin.Controllers
{
  [AdminAuthorize]
  public class UsersController : Web.Controllers.Common.ControllerBase
  {
    private IUserService UserService { get; }
    public UsersController(ApplicationUserManager userManager, IUserService userService) : base(userManager)
    {
      UserService = userService;
    }

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> ChangeEmail(ChangeEmailModel model)
    {
      await UserService.ChangeEmail(model.UserId, model.NewEmail);
      return RedirectToAction("Details", "User", new {area = "", model.UserId});
    }
  }
}