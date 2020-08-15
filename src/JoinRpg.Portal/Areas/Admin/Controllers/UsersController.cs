using System.Threading.Tasks;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers
{
    [AdminAuthorize]
    [Area("Admin")]
    public class UsersController : JoinRpg.Portal.Controllers.Common.ControllerBase
    {
        private IUserService UserService { get; }
        private ApplicationUserManager UserManager { get; }

        public UsersController(ApplicationUserManager userManager,
            IUserService userService,
            IUserRepository userRepository)
        {
            UserManager = userManager;
            UserService = userService;
        }

        [ValidateAntiForgeryToken, HttpPost]
        public async Task<ActionResult> ChangeEmail(ChangeEmailModel model)
        {
            await UserService.ChangeEmail(model.UserId, model.NewEmail);
            var user = await UserManager.FindByIdAsync(model.UserId.ToString());
            await UserManager.UpdateSecurityStampAsync(user);
            return RedirectToUserDetails(model.UserId);
        }

        private ActionResult RedirectToUserDetails(int userId) => RedirectToAction("Details", "User", new { area = "", userId });

        [ValidateAntiForgeryToken, HttpPost]
        public async Task<ActionResult> GrantAmin(int userId)
        {
            await UserService.SetAdminFlag(userId, administratorFlag: true);
            var user = await UserManager.FindByIdAsync(userId.ToString());
            await UserManager.UpdateSecurityStampAsync(user);
            return RedirectToUserDetails(userId);
        }

        [ValidateAntiForgeryToken, HttpPost]
        public async Task<ActionResult> RevokeAdmin(int userId)
        {
            await UserService.SetAdminFlag(userId, administratorFlag: false);
            var user = await UserManager.FindByIdAsync(userId.ToString());
            await UserManager.UpdateSecurityStampAsync(user);
            return RedirectToUserDetails(userId);
        }
    }
}
