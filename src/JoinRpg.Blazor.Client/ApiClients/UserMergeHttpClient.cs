using JoinRpg.Web.AdminTools.UserMerge;

namespace JoinRpg.Blazor.Client.ApiClients;

public class UserMergeHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserMergeClient
{
    // Причина отказа (аккаунт не найден, объединить нельзя) приходит JSON-строкой — панель покажет её админу
    public async Task<UserMergePreviewViewModel> GetPreview(string primaryUserLink, string secondaryUserLink)
        => await (await httpClient.GetAsync(
            $"webapi/UserMerge/GetPreview?primaryUserLink={Uri.EscapeDataString(primaryUserLink)}&secondaryUserLink={Uri.EscapeDataString(secondaryUserLink)}"))
            .ReadFromJsonOrThrowProblem<UserMergePreviewViewModel>();

    public async Task Merge(UserIdentification primary, UserIdentification secondary)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserMerge/Merge?primary={primary.Value}&secondary={secondary.Value}", content: null);
        await response.EnsureSuccessOrThrowProblem();
    }
}
