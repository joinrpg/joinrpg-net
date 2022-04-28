using JoinRpg.Interfaces;
using JoinRpg.Portal.Identity;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.UserProfile;

[Authorize]
public class AvatarController : Controller
{
    private readonly ICurrentUserAccessor currentUserAccessor;
    private readonly IAvatarService avatarService;
    private readonly ApplicationSignInManager signInManager;
    private readonly ApplicationUserManager userManager;

    public AvatarController(
        ICurrentUserAccessor currentUserAccessor,
        IAvatarService avatarService,
        ApplicationSignInManager signInManager,
        ApplicationUserManager userManager)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.avatarService = avatarService;
        this.signInManager = signInManager;
        this.userManager = userManager;
    }

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

    private async Task RefreshUserProfile()
    {
        var user = await userManager.FindByIdAsync(currentUserAccessor.UserId.ToString());
        await signInManager.RefreshSignInAsync(user);
    }
}
