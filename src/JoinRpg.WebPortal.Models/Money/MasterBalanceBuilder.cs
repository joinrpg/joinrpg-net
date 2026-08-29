using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.Models;

public static class MasterBalanceBuilder
{
    public static IReadOnlyCollection<UserIdentification> GetMasterIds(
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers)
        => masterOperations.Select(fo => fo.PaymentType?.UserId)
            .Where(userId => userId != null)
            .Select(userId => userId!.Value)
            .Concat(masterTransfers.Select(mt => mt.ReceiverId))
            .Concat(masterTransfers.Select(mt => mt.SenderId))
            .Distinct()
            .Select(userId => new UserIdentification(userId))
            .ToArray();

    public static IReadOnlyCollection<MasterBalanceViewModel> ToMasterBalanceViewModels(
        IReadOnlyCollection<UserInfo> masters,
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers,
        int projectId)
    {
        var summary = masters.Select(master =>
                new MasterBalanceViewModel(master, projectId, masterOperations, masterTransfers))
            .Where(fr => fr.AnythingEverHappens())
            .OrderBy(fr => fr.Master.DisplayName.DisplayName);
        return summary.ToArray();
    }
}
