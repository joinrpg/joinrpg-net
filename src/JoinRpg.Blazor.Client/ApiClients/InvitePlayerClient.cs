using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Blazor.Client.ApiClients;

public class InvitePlayerClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IInvitePlayerClient
{
    public async Task<ClaimIdentification> InvitePlayer(CharacterIdentification CharacterId, string UserLink, string ClaimText)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);

        var response = await httpClient.PostAsync(
            $"webapi/invite-player/Invite?projectId={CharacterId.ProjectId}&targetId={CharacterId}&UserLink={UserLink}&claimText={ClaimText}", null);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<ClaimIdentification>();
        return result ?? throw new Exception("Не удалось получить результат с сервера");
    }
}
