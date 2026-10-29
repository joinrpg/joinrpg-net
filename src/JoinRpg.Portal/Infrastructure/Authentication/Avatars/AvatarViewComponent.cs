using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

public class AvatarViewComponent(IAvatarLoader avatarProvider, IUserRepository userRepository) : ViewComponent
{
    /// <param name="userId">
    /// Чью аватарку дочитать из базы, если <paramref name="userAvatarIdOrNull"/> не пришёл. Нужен только шапке:
    /// там id берётся из cookie, а её пересобирает валидатор штампа через несколько секунд после того, как аватарку
    /// завели. Кто взял id из базы (комментарий, список аватарок), его не передаёт — второй запрос вернул бы то же.
    /// </param>
    public async Task<IViewComponentResult> InvokeAsync(
        AvatarIdentification? userAvatarIdOrNull,
        int recommendedSize,
        int? userId = null)
    {
        // Только читаем — на GET аватарку не заводим, она есть у каждого пользователя (#5602).
        var userAvatarId = userAvatarIdOrNull
            ?? (userId is int id ? (await userRepository.GetUserInfoHeaders([new UserIdentification(id)])).SingleOrDefault()?.Avatar : null);
        if (userAvatarId is null)
        {
            return Content("");
        }
        var avatar = await avatarProvider.GetAvatar(userAvatarId, recommendedSize);

        return View("Avatar", avatar);
    }
}
