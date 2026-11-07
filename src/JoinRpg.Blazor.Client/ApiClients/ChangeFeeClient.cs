using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ChangeFeeClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IChangeFeeClient
{
    public async Task ChangeFee(ClaimIdentification claimId, ChangeFeeRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(
            $"webapi/{claimId.ProjectId.Value}/claims/{claimId.ClaimId}/change-fee",
            request);

        await response.EnsureSuccessOrThrowProblem();
    }
}
