using JoinRpg.Helpers.Web;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

#nullable enable
public class AvatarViewComponent : ViewComponent
{
    private readonly IAvatarLoader avatarProvider;
    private readonly IAvatarService avatarService;

    public AvatarViewComponent(IAvatarLoader avatarProvider, IAvatarService avatarService)
    {
        this.avatarProvider = avatarProvider;
        this.avatarService = avatarService;
    }

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
                recommendedSize,
                recommendedSize);
        }
        return View("Avatar", avatar);
    }
}
