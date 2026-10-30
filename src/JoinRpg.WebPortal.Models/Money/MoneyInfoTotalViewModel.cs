using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Interfaces;

namespace JoinRpg.Web.Models;

public class MoneyInfoTotalViewModel(ProjectInfo project,
    IReadOnlyCollection<MoneyTransfer> transfers,
    IReadOnlyCollection<FinanceOperation> operations,
    PaymentTypeSummaryViewModel[] payments,
    ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<UserInfo> masters)
{
    public int ProjectId { get; } = project.ProjectId;

    public IReadOnlyCollection<MasterBalanceViewModel> Balance { get; } = MasterBalanceBuilder.ToMasterBalanceViewModels(masters, operations, transfers, project);

    public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; } = payments;

    public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; set; } = transfers.Select(transfer =>
            new MoneyTransferListItemViewModel(transfer, currentUserId, project)).ToArray();
}
