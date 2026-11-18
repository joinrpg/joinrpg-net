using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;

namespace JoinRpg.WebPortal.Managers.Claims;

/// <summary>
/// Серверная часть резолва ссылки на пользователя для <c>JoinUserLinkEditor</c>.
/// </summary>
/// <remarks>
/// В ответ уходит только <see cref="UserLinkViewModel"/> — id и отображаемое имя. Брать
/// <c>UserProfileDetailsViewModel</c> «чтобы красиво» нельзя: он отдаёт email, телефон и соцсети,
/// а знание ссылки доступа к контактам не даёт.
/// </remarks>
internal class UserLinkResolveViewService(
    IUserLinkResolver userLinkResolver,
    IUserRepository userRepository)
    : IUserLinkResolveClient
{
    public async Task<UserLinkViewModel> ResolveUserLink(string userLink)
    {
        if (string.IsNullOrWhiteSpace(userLink))
        {
            throw new FormatException("Не указана ссылка на пользователя.");
        }

        // Существование пользователя проверяет сам резолвер — тут только достаём отображаемое имя.
        var userId = await userLinkResolver.ResolveAsync(userLink.Trim());

        var user = await userRepository.GetRequiredUserInfoHeader(userId);

        return new UserLinkViewModel(user);
    }
}
