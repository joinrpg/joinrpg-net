using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Claims.Finance;

namespace JoinRpg.WebPortal.Managers.Claims;

internal class MoneyTransferViewService(IFinanceService financeService) : IMoneyTransferClient
{
    public Task Approve(MoneyTransferIdentification transferId) => Mark(transferId, approved: true);

    public Task Decline(MoneyTransferIdentification transferId) => Mark(transferId, approved: false);

    private Task Mark(MoneyTransferIdentification transferId, bool approved)
        => financeService.MarkTransfer(new ApproveRejectTransferRequest()
        {
            ProjectId = transferId.ProjectId,
            MoneyTranferId = transferId.MoneyTransferId,
            Approved = approved,
        });
}
