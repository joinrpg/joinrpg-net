using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Helpers;

namespace JoinRpg.Web.Models;

public static class MasterBalanceBuilder
{
    public static IReadOnlyCollection<UserIdentification> GetMasterIds(
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers,
        IReadOnlyCollection<ExpenseInfo> expenses,
        ProjectInfo projectInfo)
        => masterOperations.Select(fo => fo.GetPaymentMasterId(projectInfo))
            .WhereNotNull()
            .Concat(masterTransfers.Select(mt => new UserIdentification(mt.ReceiverId)))
            .Concat(masterTransfers.Select(mt => new UserIdentification(mt.SenderId)))
            .Concat(expenses.Select(e => e.MasterId))
            .Distinct()
            .ToArray();

    public static IReadOnlyCollection<MasterBalanceViewModel> ToMasterBalanceViewModels(
        IReadOnlyCollection<UserInfo> masters,
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers,
        IReadOnlyCollection<ExpenseInfo> expenses,
        ProjectInfo projectInfo)
    {
        var summary = masters.Select(master =>
                new MasterBalanceViewModel(master, projectInfo, masterOperations, masterTransfers, expenses))
            .Where(fr => fr.AnythingEverHappens())
            .OrderBy(fr => fr.Master.DisplayName.DisplayName);
        return summary.ToArray();
    }
}
