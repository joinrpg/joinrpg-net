using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.ProjectCommon.Claims;

namespace JoinRpg.Blazor.Client.ApiClients;

public class ClaimHttpClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IClaimListClient, IClaimOperationClient, IUnifiedGridClient
{
    public async Task AllowSensitiveData(ProjectIdentification projectId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/ClaimOperations/AllowSensitiveData?projectId={projectId}", content: null);

        response.EnsureSuccessStatusCode();

    }

    async Task<IReadOnlyCollection<ClaimLinkViewModel>> IClaimListClient.GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
        => await httpClient.GetFromJsonAsync<IReadOnlyCollection<ClaimLinkViewModel>>(
            $"webapi/claim-list/GetClaims?projectId={projectId.Value}&claimStatusSpec={claimStatusSpec}")
            ?? throw new Exception("Couldn't get result from server");
    async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> IUnifiedGridClient.GetForCaptain(ProjectIdentification projectId, UgStatusFilterView filter)
                => await httpClient.GetFromJsonAsync<IReadOnlyCollection<UgItemForCaptainViewModel>>(
            $"webapi/claim-list/GetForCaptain?projectId={projectId.Value}&filter={filter}")
            ?? throw new Exception("Couldn't get result from server");
}
