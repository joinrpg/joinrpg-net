using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
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
        ICurrentUserAccessor currentUserId)
    {

        var masters = operations.Select(fo => fo.PaymentType?.User)
            .Union(transfers.Select(mt => mt.Receiver))
            .Union(transfers.Select(mt => mt.Sender))
            .Distinct();

        ProjectId = project.ProjectId;

        Operations = new FinOperationListViewModel(project.ProjectId, urlHelper, operations);

        Balance = MasterBalanceBuilder.ToMasterBalanceViewModels(operations, transfers, project.ProjectId);

        Transfers = transfers.Select(transfer =>
            new MoneyTransferListItemViewModel(transfer, currentUserId)).ToArray();

        PaymentTypeSummary = payments;
    }

}
