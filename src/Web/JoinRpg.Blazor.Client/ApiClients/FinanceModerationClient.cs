using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Blazor.Client.ApiClients;

public class FinanceModerationClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IFinanceModerationClient
{
    public async Task Approve(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
        => await Post("approve", operationId, request);

    public async Task Decline(FinanceOperationIdentification operationId, FinanceModerationDecisionRequest request)
        => await Post("decline", operationId, request);

    private async Task Post<TRequest>(string action, FinanceOperationIdentification operationId, TRequest request)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsJsonAsync(
            $"webapi/{operationId.ProjectId.Value}/finance-moderation/{action}"
                + $"?claimId={operationId.ClaimId.ClaimId}&operationId={operationId.FinanceOperationId}",
            request);

        // Причина отказа (операция уже обработана, нет прав, отклонение без текста) приходит текстом, чтобы диалог показал её мастеру
        await response.EnsureSuccessOrThrowProblem();
    }
}
