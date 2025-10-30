using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes.ProjectMetadata;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Models.Money;

namespace JoinRpg.Web.Models;

public class MoneyInfoForUserViewModel(IReadOnlyCollection<MoneyTransfer> transfers,
    User master,
    IUriService urlHelper,
    IReadOnlyCollection<FinanceOperation> operations,
    PaymentTypeSummaryViewModel[] payments,
    ICurrentUserAccessor currentUserId,
    ProjectInfo projectInfo)
{
    public UserProfileDetailsViewModel UserDetails { get; } = new UserProfileDetailsViewModel(master.GetUserInfo(), projectInfo, currentUserId);
    public int ProjectId { get; } = projectInfo.ProjectId;
    public IReadOnlyCollection<MoneyTransferListItemViewModel> Transfers { get; } = transfers
            .OrderBy(f => f.Id)
            .Select(f => new MoneyTransferListItemViewModel(f, currentUserId)).ToArray();

    public FinOperationListViewModel Operations { get; } = new FinOperationListViewModel(projectInfo.ProjectId, urlHelper, operations);

    public MasterBalanceViewModel Balance { get; } = new MasterBalanceViewModel(master, projectInfo.ProjectId, operations, transfers);

    public IReadOnlyCollection<PaymentTypeSummaryViewModel> PaymentTypeSummary { get; } = payments;
}
