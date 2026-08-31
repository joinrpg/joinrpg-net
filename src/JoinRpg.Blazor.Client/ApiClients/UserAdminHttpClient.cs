using JoinRpg.Web.AdminTools;

namespace JoinRpg.Blazor.Client.ApiClients;

public class UserAdminHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IUserAdminClient
{
    public async Task<UserAdminPanelViewModel> GetAdminPanel(UserIdentification userId)
        => await httpClient.GetFromJsonAsync<UserAdminPanelViewModel>($"webapi/UserAdmin/GetAdminPanel?userId={userId.Value}")
            ?? throw new Exception("Couldn't get result from server");

    public async Task RemoveVkLink(UserIdentification userId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserAdmin/RemoveVkLink?userId={userId.Value}", content: null);

        response.EnsureSuccessStatusCode();
    }

    public async Task SetAdminFlag(UserIdentification userId, bool value)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserAdmin/SetAdminFlag?userId={userId.Value}&value={value}", content: null);

        response.EnsureSuccessStatusCode();
    }

    public async Task SetVerificationFlag(UserIdentification userId, bool value)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserAdmin/SetVerificationFlag?userId={userId.Value}&value={value}", content: null);

        response.EnsureSuccessStatusCode();
    }

    public async Task ChangeEmail(UserIdentification userId, string newEmail)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/UserAdmin/ChangeEmail?userId={userId.Value}&newEmail={Uri.EscapeDataString(newEmail)}", content: null);

        response.EnsureSuccessStatusCode();
    }
}
