using JoinRpg.Interfaces;
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

    public AvatarController(
        ICurrentUserAccessor currentUserAccessor,
        IAvatarService avatarService)
    {
        this.currentUserAccessor = currentUserAccessor;
        this.avatarService = avatarService;
    }

    [HttpPost("manage/avatars/choose")]
    public async Task<IActionResult> ChooseAvatar(int userAvatarId)
    {
        await avatarService.SelectAvatar(
            currentUserAccessor.UserId,
            new AvatarIdentification(userAvatarId)
            );
        return RedirectToAction("SetupProfile", "Manage");
    }

    [HttpPost("manage/avatars/delete")]
    public async Task<IActionResult> DeleteAvatar(int userAvatarId)
    {
        await avatarService.DeleteAvatar(
            currentUserAccessor.UserId,
            new AvatarIdentification(userAvatarId)
            );
        return RedirectToAction("SetupProfile", "Manage");
    }
}
