using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Users;
using JoinRpg.Interfaces;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models;

public class MoneyInfoTotalViewModel
{
    public int ProjectId { get; }

    public FinOperationListViewModel Operations { get; }

    public IReadOnlyCollection<MasterBalanceViewModel> Balance { get; }

    public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; }

    public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; set; }

    public MoneyInfoTotalViewModel(ProjectInfo project,
        IReadOnlyCollection<MoneyTransfer> transfers,
        IUriService urlHelper,
        IReadOnlyCollection<FinanceOperation> operations,
        PaymentTypeSummaryViewModel[] payments,
        ICurrentUserAccessor currentUserId,
        IReadOnlyCollection<UserInfo> masters)
    {
        ProjectId = project.ProjectId;

        Operations = new FinOperationListViewModel(project.ProjectId, urlHelper, operations);

        Balance = MasterBalanceBuilder.ToMasterBalanceViewModels(masters, operations, transfers, project.ProjectId);

        Transfers = transfers.Select(transfer =>
            new MoneyTransferListItemViewModel(transfer, currentUserId)).ToArray();

        PaymentTypeSummary = payments;
    }

}
