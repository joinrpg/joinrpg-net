using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

public class AvatarViewComponent(IAvatarLoader avatarProvider, IAvatarService avatarService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AvatarIdentification? userAvatarIdOrNull,
        int recommendedSize,
        int userId)
    {
        var userAvatarId = userAvatarIdOrNull ?? await avatarService.EnsureAvatarPresent(userId);
        var avatar = await avatarProvider.GetAvatar(userAvatarId, recommendedSize);

        return View("Avatar", avatar);
    }
}
