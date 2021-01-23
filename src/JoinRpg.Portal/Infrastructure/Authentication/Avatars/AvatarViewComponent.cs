using System;
using System.Threading.Tasks;
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
                    new Uri($"https://www.gravatar.com/avatar/{email}?d=identicon&s={recommendedSize}"),
                    recommendedSize,
                    recommendedSize);
            }
            return View("Avatar", avatar);
        }
    }
}
