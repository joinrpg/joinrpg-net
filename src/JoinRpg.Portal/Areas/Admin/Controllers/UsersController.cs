using Joinrpg.Web.Identity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Areas.Admin.Models;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Areas.Admin.Controllers;

[AdminAuthorize]
[Area("Admin")]
public class UsersController : JoinRpg.Portal.Controllers.Common.JoinMvcControllerBase
{
    private IUserService UserService { get; }
    private JoinUserManager UserManager { get; }

    public UsersController(JoinUserManager userManager,
        IUserService userService,
        IUserRepository userRepository)
    {
        UserManager = userManager;
        UserService = userService;
    }

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> ChangeEmail(ChangeEmailModel model)
    {
        var user = await UserManager.FindByIdAsync(model.UserId.ToString());
        var token = await UserManager.GenerateChangeEmailTokenAsync(user, model.NewEmail);
        var result = await UserManager.ChangeEmailAsync(user, model.NewEmail, token);
        if (!result.Succeeded)
        {
            return new ContentResult { Content = $"Ошибка!, {result}" };
        }
        return RedirectToUserDetails(model.UserId);
    }

    private RedirectToActionResult RedirectToUserDetails(int userId) => RedirectToAction("Details", "User", new { area = "", userId });

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> GrantAmin(int userId)
    {
        await UserService.SetAdminFlag(userId, administratorFlag: true);
        var user = await UserManager.FindByIdAsync(userId.ToString());
        _ = await UserManager.UpdateSecurityStampAsync(user);
        return RedirectToUserDetails(userId);
    }

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> RevokeAdmin(int userId)
    {
        await UserService.SetAdminFlag(userId, administratorFlag: false);
        var user = await UserManager.FindByIdAsync(userId.ToString());
        _ = await UserManager.UpdateSecurityStampAsync(user);
        return RedirectToUserDetails(userId);
    }

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> GrantVerification(int userId)
    {
        await UserService.SetVerificationFlag(userId, verificationFlag: true);
        return RedirectToUserDetails(userId);
    }

    [ValidateAntiForgeryToken, HttpPost]
    public async Task<ActionResult> RevokeVerification(int userId)
    {
        await UserService.SetVerificationFlag(userId, verificationFlag: false);
        return RedirectToUserDetails(userId);
    }
}
