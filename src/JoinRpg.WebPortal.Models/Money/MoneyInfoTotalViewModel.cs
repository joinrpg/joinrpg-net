using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Interfaces;

namespace JoinRpg.Web.Models;

public class MoneyInfoTotalViewModel(ProjectInfo project,
    IReadOnlyCollection<MoneyTransfer> transfers,
    IReadOnlyCollection<FinanceOperation> operations,
    IReadOnlyCollection<ExpenseInfo> expenses,
    PaymentTypeSummaryViewModel[] payments,
    ICurrentUserAccessor currentUserId,
    IReadOnlyCollection<UserInfo> masters)
{
    public int ProjectId { get; } = project.ProjectId;

    public IReadOnlyCollection<MasterBalanceViewModel> Balance { get; } = MasterBalanceBuilder.ToMasterBalanceViewModels(masters, operations, transfers, expenses, project);

    public ExpenseSummaryViewModel? ExpenseSummary { get; } = expenses.Count > 0 ? ExpenseSummaryBuilder.Build(expenses) : null;

    public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; } = payments;

    public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; set; } = transfers.Select(transfer =>
            new MoneyTransferListItemViewModel(transfer, currentUserId, project)).ToArray();
}
