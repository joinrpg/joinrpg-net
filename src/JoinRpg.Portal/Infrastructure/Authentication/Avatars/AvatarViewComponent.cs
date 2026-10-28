using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

public class AvatarViewComponent(IAvatarLoader avatarProvider, IUserRepository userRepository) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(
        AvatarIdentification? userAvatarIdOrNull,
        int recommendedSize,
        int userId)
    {
        // Без id аватарки приходит шапка, пока cookie не пересобрана валидатором штампа (несколько секунд после
        // того, как аватарку завели), и комментарий автора, у которого её нет. Тогда только читаем — на GET
        // аватарку не заводим, она есть у каждого пользователя (#5602).
        var userAvatarId = userAvatarIdOrNull ?? (await userRepository.GetUserInfo(new UserIdentification(userId)))?.SelectedAvatarId;
        if (userAvatarId is null)
        {
            return Content("");
        }
        var avatar = await avatarProvider.GetAvatar(userAvatarId, recommendedSize);

        return View("Avatar", avatar);
    }
}
