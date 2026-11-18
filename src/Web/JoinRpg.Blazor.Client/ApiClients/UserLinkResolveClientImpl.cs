namespace JoinRpg.Blazor.Client.ApiClients;

internal class UserLinkResolveClientImpl(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserLinkResolveClient
{
    public async Task<UserLinkViewModel> ResolveUserLink(string userLink)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);

        var response = await httpClient.PostAsync(
            $"webapi/user-link/Resolve?userLink={Uri.EscapeDataString(userLink)}",
            null);

        // «Пользователь не найден», «неверный формат ссылки» приходят JSON-строкой — редактор покажет их как есть
        return await response.ReadFromJsonOrThrowProblem<UserLinkViewModel>();
    }
}
