using System.Net;
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

        // Причина отказа приходит текстом, чтобы кнопки могли показать её мастеру
        if (response.StatusCode == HttpStatusCode.BadRequest)
        {
            throw new InvalidOperationException(await response.Content.ReadAsStringAsync());
        }

        _ = response.EnsureSuccessStatusCode();
    }
}
