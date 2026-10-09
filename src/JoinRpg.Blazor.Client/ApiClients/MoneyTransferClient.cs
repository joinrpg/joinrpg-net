using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.Blazor.Client.ApiClients;

public class MoneyTransferClient(HttpClient httpClient, CsrfTokenProvider csrfTokenProvider) : IMoneyTransferClient
{
    public async Task Approve(MoneyTransferIdentification transferId) => await Post("approve", transferId);

    public async Task Decline(MoneyTransferIdentification transferId) => await Post("decline", transferId);

    private async Task Post(string action, MoneyTransferIdentification transferId)
    {
        await csrfTokenProvider.SetCsrfToken(httpClient);
        var response = await httpClient.PostAsync(
            $"webapi/{transferId.ProjectId.Value}/money-transfer/{action}?transferId={transferId.MoneyTransferId}",
            content: null);
        _ = response.EnsureSuccessStatusCode();
    }
}
