using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Impl;
using JoinRpg.Services.Interfaces.Avatars;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

public class AvatarViewComponent(IAvatarLoader avatarProvider, IAvatarService avatarService) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AvatarIdentification? userAvatarIdOrNull,
        string email,
        int recommendedSize,
        int userId)
    {
        AvatarInfo avatar;
        if (userAvatarIdOrNull is AvatarIdentification userAvatarId)
        {
            avatar = await avatarProvider.GetAvatar(userAvatarId, recommendedSize);
        }
        else
        {
            _ = Task.Run(() => avatarService.AddGrAvatarIfRequired(userId));
            avatar = new AvatarInfo(
                GravatarHelper.GetLink(email, recommendedSize),
                recommendedSize);
        }
        return View("Avatar", avatar);
    }
}
