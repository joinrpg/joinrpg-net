namespace JoinRpg.Blazor.Client.ApiClients;

internal class UserLinkResolveClientImpl(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserLinkResolveClient
{
    public async Task<UserLinkViewModel> ResolveUserLink(string userLink)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);

        var response = await httpClient.PostAsync(
            $"webapi/user-link/Resolve?userLink={Uri.EscapeDataString(userLink)}",
            null);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync();
            throw new Exception(
                string.IsNullOrWhiteSpace(errorMessage)
                    ? "Не удалось определить пользователя"
                    : errorMessage.Trim('"'));
        }

        return await response.Content.ReadFromJsonAsync<UserLinkViewModel>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }
}
