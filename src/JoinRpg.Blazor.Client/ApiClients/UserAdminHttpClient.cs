using JoinRpg.Web.AdminTools;

namespace JoinRpg.Blazor.Client.ApiClients;

public class UserAdminHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserAdminClient
{
    public async Task<UserAdminPanelViewModel> GetAdminPanel(UserIdentification userId)
        => await (await httpClient.GetAsync($"webapi/UserAdmin/GetAdminPanel?userId={userId.Value}"))
            .ReadFromJsonOrThrowProblem<UserAdminPanelViewModel>();

    public Task RemoveVkLink(UserIdentification userId)
        => Post($"webapi/UserAdmin/RemoveVkLink?userId={userId.Value}");

    public Task SetAdminFlag(UserIdentification userId, bool value)
        => Post($"webapi/UserAdmin/SetAdminFlag?userId={userId.Value}&value={value}");

    public Task SetVerificationFlag(UserIdentification userId, bool value)
        => Post($"webapi/UserAdmin/SetVerificationFlag?userId={userId.Value}&value={value}");

    public Task ChangeEmail(UserIdentification userId, string newEmail)
        => Post($"webapi/UserAdmin/ChangeEmail?userId={userId.Value}&newEmail={Uri.EscapeDataString(newEmail)}");

    public Task SetBirthDate(UserIdentification userId, DateOnly? birthDate)
    {
        var query = birthDate is null ? "" : $"&birthDate={birthDate.Value:yyyy-MM-dd}";
        return Post($"webapi/UserAdmin/SetBirthDate?userId={userId.Value}{query}");
    }

    private async Task Post(string uri)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(uri, content: null);

        // Отказ (например, нет прав администратора) панель показывает как есть
        await response.EnsureSuccessOrThrowProblem();
    }
}
