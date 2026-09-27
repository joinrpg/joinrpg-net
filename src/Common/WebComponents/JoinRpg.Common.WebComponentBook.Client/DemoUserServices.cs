using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Common.WebComponents;

namespace JoinRpg.Common.WebComponentBook.Client;

/// <summary>
/// Фальшивый резолв ссылки на пользователя: в каталоге компонентов нет ни сервера, ни базы,
/// а показать поведение редактора надо.
/// </summary>
internal sealed class DemoUserLinkResolveClient : IUserLinkResolveClient
{
    public Task<UserLinkViewModel> ResolveUserLink(int projectId, string userLink)
    {
        if (int.TryParse(userLink, out var id) && id > 0)
        {
            return Task.FromResult(new UserLinkViewModel(new UserIdentification(id), $"Пользователь {id}", ViewMode.Show));
        }

        if (userLink.Contains("vk.com/id", StringComparison.OrdinalIgnoreCase))
        {
            return Task.FromResult(new UserLinkViewModel(new UserIdentification(456), "Вася Пупкин", ViewMode.Show));
        }

        throw new InvalidOperationException($"Пользователь по ссылке «{userLink}» не найден.");
    }
}

/// <summary>
/// Ссылки на профили в каталоге компонентов ведут на боевой сайт — своего роутинга профилей тут нет.
/// </summary>
internal sealed class DemoUserLinkLocator : IUriLocator<UserLinkViewModel>
{
    public Uri GetUri(UserLinkViewModel target) => new($"https://joinrpg.ru/user/{target.UserId?.Value}");
}
