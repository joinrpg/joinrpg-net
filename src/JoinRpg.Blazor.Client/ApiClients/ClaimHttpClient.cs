using JoinRpg.Web.Claims;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimClient
{
    public async Task AllowSensitiveData(ProjectIdentification projectId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/ClaimOperations/AllowSensitiveData?projectId={projectId}", content: null);

        response.EnsureSuccessStatusCode();

    }
}
