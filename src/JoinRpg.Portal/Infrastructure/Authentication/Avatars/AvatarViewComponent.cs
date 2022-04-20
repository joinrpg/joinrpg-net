using JoinRpg.Helpers.Web;
using JoinRpg.PrimitiveTypes;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars
{
#nullable enable
    public class AvatarViewComponent : ViewComponent
    {
        private readonly IAvatarLoader avatarProvider;

        public AvatarViewComponent(IAvatarLoader avatarProvider)
            => this.avatarProvider = avatarProvider;

        public async Task<IViewComponentResult> InvokeAsync(
            AvatarIdentification? userAvatarIdOrNull,
            string email,
            int recommendedSize)
        {
            AvatarInfo avatar;
            if (userAvatarIdOrNull is AvatarIdentification userAvatarId)
            {
                avatar = await avatarProvider.GetAvatar(userAvatarId, recommendedSize);
            }
            else
            {
                avatar = new AvatarInfo(
                    GravatarHelper.GetLink(email, recommendedSize),
                    recommendedSize,
                    recommendedSize);
            }
            return View("Avatar", avatar);
        }
    }
}
