using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.Web.Models;

public class MasterBalanceViewModel
{
    [Display(Name = "Мастер")]
    public UserInfo Master { get; }

    [Display(Name = "Денег на руках")]
    public decimal Total { get; }

    [Display(Name = "Получено от других мастеров")]
    public int ReceiveBalance { get; }
    [Display(Name = "Отправлено другим мастерам")]
    public int SendBalance { get; }
    [Display(Name = "Собрано взносов")]
    public int FeeBalance { get; }
    [Display(Name = "Собрано взносов (не подтверждено)")]
    public int ModerationBalance { get; }

    /// <summary>Расходы мастера, отрицательные или 0 (деньги уходят).</summary>
    [Display(Name = "Расходы")]
    public decimal ExpensesBalance { get; }
    /// <summary>Запланированные расходы мастера, отрицательные или 0 (деньги ещё не ушли).</summary>
    [Display(Name = "Запланировано")]
    public decimal PlannedExpenses { get; }

    public int ProjectId { get; }

    public MasterBalanceViewModel(
        UserInfo master,
        ProjectInfo projectInfo,
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers,
        IReadOnlyCollection<ExpenseInfo>? expenses = null)
    {
        ArgumentNullException.ThrowIfNull(master);
        ArgumentNullException.ThrowIfNull(masterOperations);
        ArgumentNullException.ThrowIfNull(masterTransfers);

        Master = master;
        ProjectId = projectInfo.ProjectId;
        ReceiveBalance = masterTransfers.ReceivedByMasterSum(master.UserId);
        SendBalance = masterTransfers.SendedByMasterSum(master.UserId);
        FeeBalance = masterOperations
            .Where(fo => fo.Approved)
            .Where(fo => fo.GetPaymentMasterId(projectInfo) == master.UserId)
            .Sum(fo => fo.MoneyAmount);

        ModerationBalance = masterOperations
            .Where(fo => fo.RequireModeration)
            .Where(fo => fo.GetPaymentMasterId(projectInfo) == master.UserId)
            .Sum(fo => fo.MoneyAmount);

        // Эффекты расхода на баланс (в т.ч. правило «Rejected не считается») — в доменном типе ExpenseInfo.
        var ownExpenses = (expenses ?? [])
            .Where(e => e.MasterId == master.UserId)
            .ToArray();
        ExpensesBalance = -ownExpenses.Sum(e => e.ActualBalanceEffect);
        PlannedExpenses = -ownExpenses.Sum(e => e.PlannedBalanceEffect);

        Total = FeeBalance + ReceiveBalance + SendBalance + ExpensesBalance;
    }

    public bool AnythingEverHappens() => ReceiveBalance != 0 || FeeBalance != 0 ||
                                         SendBalance != 0 || ModerationBalance != 0 ||
                                         ExpensesBalance != 0 || PlannedExpenses != 0;
}
