using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.DomainTypes.Test.Finances;

public class ExpenseInfoBalanceEffectTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static ExpenseInfo Expense(
        ExpenseState state,
        int plannedQuantity,
        decimal plannedPrice,
        int? actualQuantity = null,
        decimal? actualPrice = null)
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
        var expense = Expense(ExpenseState.Approved, 2, 100m, 2, 90m);

        expense.ActualBalanceEffect.ShouldBe(180m);
        expense.PlannedBalanceEffect.ShouldBe(0m);
    }

    [Fact]
    public void ApprovedWithoutActual_CountsIntoPlannedEffectOnly()
    {
        var expense = Expense(ExpenseState.Approved, 2, 100m);

        expense.ActualBalanceEffect.ShouldBe(0m);
        expense.PlannedBalanceEffect.ShouldBe(200m);
    }

    [Fact]
    public void ProposedWithoutActual_CountsIntoPlannedEffect()
    {
        var expense = Expense(ExpenseState.Proposed, 2, 100m);

        expense.ActualBalanceEffect.ShouldBe(0m);
        expense.PlannedBalanceEffect.ShouldBe(200m);
    }

    [Fact]
    public void ProposedWithActual_CountsActualIntoPlannedEffect()
    {
        var expense = Expense(ExpenseState.Proposed, 2, 100m, 2, 80m);

        expense.ActualBalanceEffect.ShouldBe(0m);
        expense.PlannedBalanceEffect.ShouldBe(160m);
    }

    [Fact]
    public void RejectedWithActual_CountsIntoNothing()
    {
        var expense = Expense(ExpenseState.Rejected, 2, 100m, 2, 90m);

        expense.ActualBalanceEffect.ShouldBe(0m);
        expense.PlannedBalanceEffect.ShouldBe(0m);
    }

    [Fact]
    public void ApprovedWithKopeckActual_CountsIntoActualEffectOnly()
    {
        var expense = Expense(ExpenseState.Approved, 3, 49.90m, 3, 49.90m);

        expense.ActualBalanceEffect.ShouldBe(149.70m);
        expense.PlannedBalanceEffect.ShouldBe(0m);
    }

    [Fact]
    public void ProposedWithKopeckPlan_CountsKopeckPlanIntoPlannedEffect()
    {
        var expense = Expense(ExpenseState.Proposed, 3, 49.90m);

        expense.ActualBalanceEffect.ShouldBe(0m);
        expense.PlannedBalanceEffect.ShouldBe(149.70m);
    }
}
