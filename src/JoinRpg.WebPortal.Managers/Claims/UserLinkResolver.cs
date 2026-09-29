using JoinRpg.Data.Interfaces;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class UserLinkResolver(IUserRepository userRepository) : IUserLinkResolver
{
    public async Task<UserIdentification> ResolveAsync(string userLink)
    {
        if (!UserLinkParser.TryParseSocialUserLink(userLink, out var parsed))
        {
            throw new FormatException($"Неверный формат ссылки на пользователя: '{userLink}'.");
        }

        return parsed switch
        {
            ParsedUserLink.JoinRpgUser(var userId) => await EnsureExists(userId),
            ParsedUserLink.VkProfile(var vkId) => await ResolveVk(vkId),
            ParsedUserLink.TelegramProfile(var username) => await ResolveTelegram(username),
            ParsedUserLink.EmailAddress(var email) => await ResolveEmail(email),
            _ => throw new InvalidOperationException($"Неизвестный тип ссылки: {parsed.GetType().Name}")
        };
    }

    /// <summary>
    /// Существование пользователя проверяем и для числового id: остальные виды ссылок резолвятся
    /// поиском в базе и сами отсекают несуществующих, а id раньше проходил насквозь — опечатка
    /// доезжала до мутации и падала уже внутри создания заявки, оставляя в логе сбой операции
    /// вместо понятной ошибки ввода.
    /// </summary>
    private async Task<UserIdentification> EnsureExists(UserIdentification userId)
        => (await userRepository.GetUserInfoHeaders([userId])).Any()
            ? userId
            : throw new FormatException($"Пользователь {userId.Value} не найден.");

    private async Task<UserIdentification> ResolveVk(string vkId)
        => await userRepository.FindByVk(vkId)
            ?? throw new FormatException($"Пользователь с VK профилем '{vkId}' не найден.");

    private async Task<UserIdentification> ResolveTelegram(string username)
        => await userRepository.FindByTelegram(username)
            ?? throw new FormatException($"Пользователь с Telegram @{username} не найден.");

    private async Task<UserIdentification> ResolveEmail(string email)
        => await userRepository.FindByEmail(email)
            ?? throw new FormatException($"Пользователь с email '{email}' не найден.");
}
