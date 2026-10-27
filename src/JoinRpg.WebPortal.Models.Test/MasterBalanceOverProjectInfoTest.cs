using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DataModel.Mocks;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Баланс мастера и сводка по способам оплаты находят кассу операции по метаданным проекта, а не по навигации
/// <see cref="FinanceOperation.PaymentType"/> (#5852): в операциях теста она не заполнена.
/// </summary>
public class MasterBalanceOverProjectInfoTest
{
    private readonly MockedProject mock = new();

    private FinanceOperation Operation(PaymentType paymentType, int money, FinanceOperationState state) => new()
    {
        ProjectId = mock.Project.ProjectId,
        PaymentTypeId = paymentType.PaymentTypeId,
        MoneyAmount = money,
        OperationType = FinanceOperationType.Submit,
        State = state,
    };

    [Fact]
    public void Balance_CountsOnlyOwnCash()
    {
        var ownCash = mock.CreateCashPaymentType(mock.Master);
        var otherCash = mock.CreateCashPaymentType(mock.CreateMaster());
        FinanceOperation[] operations =
        [
            Operation(ownCash, 100, FinanceOperationState.Approved),
            Operation(ownCash, 30, FinanceOperationState.Proposed),
            Operation(otherCash, 1000, FinanceOperationState.Approved),
        ];

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo, operations, Array.Empty<MoneyTransfer>());

        balance.FeeBalance.ShouldBe(100);
        balance.ModerationBalance.ShouldBe(30);
    }

    [Fact]
    public void MasterIds_TakenFromPaymentTypeOwners()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);

        var ids = MasterBalanceBuilder.GetMasterIds([Operation(cash, 100, FinanceOperationState.Approved)], [], mock.ProjectInfo);

        ids.ShouldBe([mock.MasterInfo.UserId]);
    }

    [Fact]
    public void PaymentTypeSummary_SumsOperationsOfThisPaymentType()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var otherCash = mock.CreateCashPaymentType(mock.CreateMaster());
        var paymentType = mock.ProjectInfo.ProjectFinanceSettings.PaymentTypes.Single(pt => pt.PaymentTypeId.PaymentTypeId == cash.PaymentTypeId);

        var summary = new PaymentTypeSummaryViewModel(paymentType,
        [
            Operation(cash, 100, FinanceOperationState.Approved),
            Operation(cash, 30, FinanceOperationState.Proposed),
            Operation(otherCash, 1000, FinanceOperationState.Approved),
        ]);

        summary.Total.ShouldBe(100);
        summary.Moderation.ShouldBe(30);
        summary.Master.UserId.ShouldBe(mock.MasterInfo.UserId);
    }
}
