using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.Blazor.Client.ApiClients;

public class FinanceRestoreClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IFinanceRestoreClient
{
    public async Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
        => await (await httpClient.GetAsync($"webapi/finance-restore/GetPlan?projectId={projectId.Value}"))
            .ReadFromJsonOrThrowProblem<FinanceRestorePlanViewModel>();

    public async Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/finance-restore/Apply?projectId={request.ClaimId.ProjectId.Value}",
            JsonContent.Create(request));
        return await response.ReadFromJsonOrThrowProblem<FinanceRestoreApplyResult>();
    }

    public async Task CreateOwnCash(ProjectIdentification projectId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync($"webapi/finance-restore/CreateOwnCash?projectId={projectId.Value}", content: null);
        await response.EnsureSuccessOrThrowProblem();
    }
}
