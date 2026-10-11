using JoinRpg.Web.AdminTools.UserMerge;

namespace JoinRpg.Blazor.Client.ApiClients;

public class UserMergeHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserMergeClient
{
    public async Task<UserMergePreviewViewModel> GetPreview(string primaryUserLink, string secondaryUserLink)
    {
        var response = await httpClient.GetAsync(
            $"webapi/UserMerge/GetPreview?primaryUserLink={Uri.EscapeDataString(primaryUserLink)}&secondaryUserLink={Uri.EscapeDataString(secondaryUserLink)}");
        await EnsureSuccess(response, "Не удалось проверить аккаунты");
        return await response.Content.ReadFromJsonAsync<UserMergePreviewViewModel>()
            ?? throw new Exception("Не удалось получить результат с сервера");
    }

    public async Task Merge(UserIdentification primary, UserIdentification secondary)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserMerge/Merge?primary={primary.Value}&secondary={secondary.Value}", content: null);
        await EnsureSuccess(response, "Не удалось объединить аккаунты");
    }

    private static async Task EnsureSuccess(HttpResponseMessage response, string fallbackMessage)
    {
        if (!response.IsSuccessStatusCode)
        {
            // Понятный текст отдаёт только 400 — на 500 в теле HTML страницы ошибки.
            var errorMessage = response.StatusCode == System.Net.HttpStatusCode.BadRequest
                ? await response.Content.ReadAsStringAsync()
                : null;
            throw new Exception(string.IsNullOrWhiteSpace(errorMessage) ? $"{fallbackMessage}." : errorMessage.Trim('"'));
        }
    }
}
