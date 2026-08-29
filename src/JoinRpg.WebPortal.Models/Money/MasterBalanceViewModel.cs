using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Web.Models;

public class MasterBalanceViewModel
{
    [Display(Name = "Мастер")]
    public UserInfo Master { get; }

    [Display(Name = "Денег на руках")]
    public int Total { get; }

    [Display(Name = "Получено от других мастеров")]
    public int ReceiveBalance { get; }
    [Display(Name = "Отправлено другим мастерам")]
    public int SendBalance { get; }
    [Display(Name = "Собрано взносов")]
    public int FeeBalance { get; }
    [Display(Name = "Собрано взносов (не подтверждено)")]
    public int ModerationBalance { get; }

    [Display(Name = "Расходы")]
    public int ExpensesBalance { get; } = 0; //Not used

    public int ProjectId { get; }

    public MasterBalanceViewModel(
        UserInfo master,
        int projectId,
        IReadOnlyCollection<FinanceOperation> masterOperations,
        IReadOnlyCollection<MoneyTransfer> masterTransfers)
    {
        ArgumentNullException.ThrowIfNull(master);
        ArgumentNullException.ThrowIfNull(masterOperations);
        ArgumentNullException.ThrowIfNull(masterTransfers);

        Master = master;
        ProjectId = projectId;
        ReceiveBalance = masterTransfers.ReceivedByMasterSum(master.UserId);
        SendBalance = masterTransfers.SendedByMasterSum(master.UserId);
        FeeBalance = masterOperations
            .Where(fo => fo.Approved)
            .Where(fo => fo.PaymentType?.UserId == master.UserId.Value)
            .Sum(fo => fo.MoneyAmount);

        ModerationBalance = masterOperations
            .Where(fo => fo.RequireModeration)
            .Where(fo => fo.PaymentType?.UserId == master.UserId.Value)
            .Sum(fo => fo.MoneyAmount);

        Total = FeeBalance + ReceiveBalance + SendBalance;
    }

    public bool AnythingEverHappens() => ReceiveBalance != 0 || FeeBalance != 0 ||
                                         SendBalance != 0 || ModerationBalance != 0 ||
                                         ExpensesBalance != 0;
}
