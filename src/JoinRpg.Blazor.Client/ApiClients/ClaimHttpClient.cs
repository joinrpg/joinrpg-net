using JoinRpg.DomainTypes.Characters.Claims;
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

        await response.EnsureSuccessOrThrowProblem();
    }

    public async Task AcceptInvitation(ClaimIdentification claimId, AcceptInvitationRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync($"webapi/ClaimOperations/AcceptInvitation?claimId={claimId}", request);

        await response.EnsureSuccessOrThrowProblem();
    }

    async Task<IReadOnlyCollection<ClaimLinkViewModel>> IClaimListClient.GetClaims(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec)
        => await (await httpClient.GetAsync(
            $"webapi/claim-list/GetClaims?projectId={projectId.Value}&claimStatusSpec={claimStatusSpec}"))
            .ReadFromJsonOrThrowProblem<IReadOnlyCollection<ClaimLinkViewModel>>();

    async Task<UgPageViewModel> IUnifiedGridClient.GetGrid(ProjectIdentification projectId, UgScopeView scope, UgFilterView filter, int page)
        => await (await httpClient.GetAsync(
            $"webapi/claim-list/GetGrid?projectId={projectId.Value}&scope={scope}&status={filter.Status}&onlyWithProblems={filter.OnlyWithProblems}&page={page}"))
            .ReadFromJsonOrThrowProblem<UgPageViewModel>();
}
