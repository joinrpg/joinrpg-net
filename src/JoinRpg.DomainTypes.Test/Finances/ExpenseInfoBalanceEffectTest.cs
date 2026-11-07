using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.DomainTypes.Test.Finances;

public class ExpenseInfoBalanceEffectTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static ExpenseInfo Expense(
        ExpenseState state,
        int plannedQuantity,
        int plannedPrice,
        int? actualQuantity = null,
        int? actualPrice = null)
        => new(
            new ExpenseIdentification(ProjectId, 1),
            "Аренда базы",
            Description: null,
            [],
            plannedQuantity,
            plannedPrice,
            actualQuantity,
            actualPrice,
            SpentAt: null,
            state,
            MasterId: new UserIdentification(2),
            CreatedById: new UserIdentification(2));

    [Fact]
    public void ApprovedWithActual_CountsIntoActualEffectOnly()
    {
        var expense = Expense(ExpenseState.Approved, 2, 100, 2, 90);

        expense.ActualBalanceEffect.ShouldBe(180);
        expense.PlannedBalanceEffect.ShouldBe(0);
    }

    [Fact]
    public void ApprovedWithoutActual_CountsIntoPlannedEffectOnly()
    {
        var expense = Expense(ExpenseState.Approved, 2, 100);

        expense.ActualBalanceEffect.ShouldBe(0);
        expense.PlannedBalanceEffect.ShouldBe(200);
    }

    [Fact]
    public void ProposedWithoutActual_CountsIntoPlannedEffect()
    {
        var expense = Expense(ExpenseState.Proposed, 2, 100);

        expense.ActualBalanceEffect.ShouldBe(0);
        expense.PlannedBalanceEffect.ShouldBe(200);
    }

    [Fact]
    public void ProposedWithActual_CountsActualIntoPlannedEffect()
    {
        var expense = Expense(ExpenseState.Proposed, 2, 100, 2, 80);

        expense.ActualBalanceEffect.ShouldBe(0);
        expense.PlannedBalanceEffect.ShouldBe(160);
    }

    [Fact]
    public void RejectedWithActual_CountsIntoNothing()
    {
        var expense = Expense(ExpenseState.Rejected, 2, 100, 2, 90);

        expense.ActualBalanceEffect.ShouldBe(0);
        expense.PlannedBalanceEffect.ShouldBe(0);
    }
}
