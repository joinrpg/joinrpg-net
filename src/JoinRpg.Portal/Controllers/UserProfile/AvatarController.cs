using Joinrpg.Web.Identity;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.UserProfile;

[Authorize]
public class AvatarController(
    ICurrentUserAccessor currentUserAccessor,
    IAvatarService avatarService,
    ApplicationSignInManager signInManager,
    JoinUserManager userManager) : Controller
{
    [HttpPost("manage/avatars/choose")]
    public async Task<IActionResult> ChooseAvatar(int userAvatarId)
    {
        await avatarService.SelectAvatar(
            currentUserAccessor.UserId,
            new AvatarIdentification(userAvatarId)
            );
        await RefreshUserProfile();
        return RedirectToAction("SetupProfile", "Manage");
    }

    [HttpPost("manage/avatars/delete")]
    public async Task<IActionResult> DeleteAvatar(int userAvatarId)
    {
        await avatarService.DeleteAvatar(
            currentUserAccessor.UserId,
            new AvatarIdentification(userAvatarId)
            );
        await RefreshUserProfile();
        return RedirectToAction("SetupProfile", "Manage");
    }

    [HttpPost("manage/avatars/recache")]
    public async Task<IActionResult> RecacheAvatar(int userAvatarId)
    {
        await avatarService.RecacheAvatar(
            currentUserAccessor.UserIdentification,
            new AvatarIdentification(userAvatarId)
            );
        await RefreshUserProfile();
        return RedirectToAction("SetupProfile", "Manage");
    }

    private async Task RefreshUserProfile()
    {
        var user = await userManager.FindByIdAsync(currentUserAccessor.UserId.ToString());
        await signInManager.RefreshSignInAsync(user);
    }
}
