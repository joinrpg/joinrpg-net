using JoinRpg.PrimitiveTypes.Claims;
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

    async Task<IReadOnlyCollection<ClaimLinkViewModel>> IClaimClient.GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
        => await httpClient.GetFromJsonAsync<IReadOnlyCollection<ClaimLinkViewModel>>(
            $"webapi/claim-list/GetClaims?projectId={projectId.Value}&claimStatusSpec={claimStatusSpec}")
            ?? throw new Exception("Couldn't get result from server");
}
