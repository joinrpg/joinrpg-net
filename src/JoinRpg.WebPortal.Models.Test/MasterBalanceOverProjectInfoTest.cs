using JoinRpg.DataModel;
using JoinRpg.DataModel.Finances;
using JoinRpg.DataModel.Mocks;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.WebPortal.Models.Test;

/// <summary>
/// Баланс мастера и сводка по способам оплаты находят кассу операции по метаданным проекта, а не по навигации
/// <see cref="FinanceOperation.PaymentType"/> (#5852): в операциях теста она не заполнена.
/// </summary>
public class MasterBalanceOverProjectInfoTest
{
    private readonly MockedProject mock = new();

    private static readonly ProjectIdentification ProjectId = new(1);

    private FinanceOperation Operation(PaymentType paymentType, int money, FinanceOperationState state) => new()
    {
        ProjectId = mock.Project.ProjectId,
        PaymentTypeId = paymentType.PaymentTypeId,
        MoneyAmount = money,
        OperationType = FinanceOperationType.Submit,
        State = state,
    };

    private static ExpenseInfo Expense(
        int masterUserId,
        ExpenseState state,
        int plannedQuantity,
        int plannedPrice,
        int? actualQuantity = null,
        int? actualPrice = null)
        => new(
            new ExpenseIdentification(ProjectId, masterUserId),
            $"Расход {masterUserId}",
            Description: null,
            [],
            plannedQuantity,
            plannedPrice,
            actualQuantity,
            actualPrice,
            SpentAt: null,
            state,
            MasterId: new UserIdentification(masterUserId),
            CreatedById: new UserIdentification(masterUserId));

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

        var ids = MasterBalanceBuilder.GetMasterIds([Operation(cash, 100, FinanceOperationState.Approved)], [], [], mock.ProjectInfo);

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

    [Fact]
    public void ApprovedExpenseWithActual_ReducesTotal()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var expenses = new[] { Expense(mock.Master.UserId, ExpenseState.Approved, 2, 100, 2, 90) };

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo,
            [Operation(cash, 500, FinanceOperationState.Approved)], [], expenses);

        balance.ExpensesBalance.ShouldBe(180);
        balance.PlannedExpenses.ShouldBe(0);
        balance.Total.ShouldBe(500 - 180);
    }

    [Fact]
    public void ApprovedExpenseWithoutActual_GoesToPlannedButNotTotal()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var expenses = new[] { Expense(mock.Master.UserId, ExpenseState.Approved, 3, 100) };

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo,
            [Operation(cash, 500, FinanceOperationState.Approved)], [], expenses);

        balance.ExpensesBalance.ShouldBe(0);
        balance.PlannedExpenses.ShouldBe(300);
        balance.Total.ShouldBe(500);
    }

    [Fact]
    public void RejectedExpense_DoesNotAffectBalance()
    {
        var cash = mock.CreateCashPaymentType(mock.Master);
        var expenses = new[] { Expense(mock.Master.UserId, ExpenseState.Rejected, 1, 100, 1, 100) };

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo,
            [Operation(cash, 500, FinanceOperationState.Approved)], [], expenses);

        balance.ExpensesBalance.ShouldBe(0);
        balance.PlannedExpenses.ShouldBe(0);
        balance.Total.ShouldBe(500);
        balance.AnythingEverHappens().ShouldBeTrue();
    }

    [Fact]
    public void ProposedExpenseWithActual_GoesToPlannedButNotExpenses()
    {
        var expenses = new[] { Expense(mock.Master.UserId, ExpenseState.Proposed, 2, 100, 2, 80) };

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo,
            [], [], expenses);

        balance.ExpensesBalance.ShouldBe(0);
        balance.PlannedExpenses.ShouldBe(160);
        balance.Total.ShouldBe(0);
    }

    [Fact]
    public void ExpensesOfOtherMaster_DoNotAffectBalance()
    {
        var other = mock.CreateMaster();
        var expenses = new[] { Expense(other.UserId, ExpenseState.Approved, 1, 100, 1, 100) };

        var balance = new MasterBalanceViewModel(mock.MasterInfo, mock.ProjectInfo,
            [], [], expenses);

        balance.ExpensesBalance.ShouldBe(0);
        balance.PlannedExpenses.ShouldBe(0);
        balance.AnythingEverHappens().ShouldBeFalse();
    }

    [Fact]
    public void MasterWithOnlyExpenses_AppearsInSummary()
    {
        var other = mock.CreateMaster();
        var expenses = new[] { Expense(other.UserId, ExpenseState.Approved, 1, 100, 1, 100) };
        var otherInfo = mock.TryGetUserInfo(new UserIdentification(other.UserId))!;

        var ids = MasterBalanceBuilder.GetMasterIds([], [], expenses, mock.ProjectInfo);
        var summary = MasterBalanceBuilder.ToMasterBalanceViewModels([mock.MasterInfo, otherInfo], [], [], expenses, mock.ProjectInfo);

        ids.ShouldBe([new UserIdentification(other.UserId)]);
        summary.ShouldHaveSingleItem();
        summary.Single().Master.UserId.ShouldBe(new UserIdentification(other.UserId));
        summary.Single().ExpensesBalance.ShouldBe(100);
        summary.Single().Total.ShouldBe(-100);
    }
}
