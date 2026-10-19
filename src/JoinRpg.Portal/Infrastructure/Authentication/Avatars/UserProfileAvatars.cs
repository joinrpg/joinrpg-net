using Joinrpg.Web.Identity;
using JoinRpg.Services.Interfaces.Avatars;
using JoinRpg.Web.UserProfile;

namespace JoinRpg.Portal.Infrastructure.Authentication.Avatars;

/// <summary>
/// Заполняет аватарки карточек профиля до рендера: <see cref="UserProfilePanel"/> только рисует готовые данные.
/// </summary>
public class UserProfileAvatars(IAvatarLoader avatarLoader, IAvatarService avatarService)
{
    private const int CardAvatarSize = 64;

    public async Task Fill(params IEnumerable<UserProfileDetailsViewModel> cards)
    {
        // По очереди: за загрузчиком и сервисом — один EF6-контекст запроса, параллельных операций он не допускает.
        foreach (var card in cards)
        {
            // У скрытого пользователя UserId нет, аватарку грузить не по кому.
            if (card.User.UserId is not { } userId)
            {
                continue;
            }
            var avatarId = card.SelectedAvatarId ?? await avatarService.EnsureAvatarPresent(userId.Value);
            card.Avatar = await avatarLoader.GetAvatar(avatarId, CardAvatarSize);
        }
    }
}
