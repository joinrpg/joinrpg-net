using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Finances;

namespace JoinRpg.WebPortal.Models.Test.Money;

public class ExpenseSummaryViewModelTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static ExpenseInfo Expense(
        int id,
        ExpenseState state,
        int plannedQuantity,
        decimal plannedPrice,
        int? actualQuantity = null,
        decimal? actualPrice = null,
        params string[] categoryPath)
        => new(
            new ExpenseIdentification(ProjectId, id),
            $"Расход {id}",
            Description: null,
            [.. categoryPath.Select((name, index) => new ExpenseCategoryBriefInfo(
                new ExpenseCategoryIdentification(ProjectId, 100 + index), name))],
            plannedQuantity,
            plannedPrice,
            actualQuantity,
            actualPrice,
            SpentAt: null,
            state,
            MasterId: new UserIdentification(2),
            CreatedById: new UserIdentification(2));

    [Fact]
    public void Build_GroupsByCategoryPath_AndSumsByState()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 2, 100, 2, 90, "Аренда базы", "Жилые корпуса"),
            Expense(2, ExpenseState.Proposed, 1, 50, categoryPath: ["Аренда базы", "Жилые корпуса"]),
            Expense(3, ExpenseState.Approved, 3, 10, categoryPath: ["Транспорт"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Count.ShouldBe(2);
        var rent = summary.Rows.First(r => r.CategoryPath == "Аренда базы\\Жилые корпуса");
        rent.ApprovedPlannedCost.ShouldBe(200);
        rent.ApprovedActualCost.ShouldBe(180);
        rent.ProposedCost.ShouldBe(50);
        var transport = summary.Rows.First(r => r.CategoryPath == "Транспорт");
        transport.ApprovedPlannedCost.ShouldBe(30);
        transport.ApprovedActualCost.ShouldBe(0);
        transport.ProposedCost.ShouldBe(0);
        summary.TotalSpent.ShouldBe(180);
    }

    [Fact]
    public void Build_ExcludesRejectedFromAllSums()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Rejected, 1, 100, 1, 100, "Аренда базы"),
            Expense(2, ExpenseState.Proposed, 2, 20, categoryPath: ["Транспорт"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Count.ShouldBe(1);
        summary.Rows.Single().CategoryPath.ShouldBe("Транспорт");
        summary.Rows.Single().ProposedCost.ShouldBe(40);
        summary.TotalSpent.ShouldBe(0);
    }

    [Fact]
    public void Build_ProposedWithActual_UsesActualCost()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Proposed, 2, 50, 2, 45, "Аренда базы"),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Single().ProposedCost.ShouldBe(90);
    }

    [Fact]
    public void Build_ApprovedWithoutActual_CountsOnlyIntoPlanned()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 3, 10, categoryPath: ["Аренда базы"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Single().ApprovedPlannedCost.ShouldBe(30);
        summary.Rows.Single().ApprovedActualCost.ShouldBe(0);
        summary.TotalSpent.ShouldBe(0);
    }

    [Fact]
    public void Build_SumsKopeckCostsAcrossExpenses()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 1, 49.90m, categoryPath: ["Аренда базы"]),
            Expense(2, ExpenseState.Approved, 1, 100.10m, categoryPath: ["Аренда базы"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        // 49,90 + 100,10 = 150,00 — копейки не теряются при суммировании
        summary.Rows.Single().ApprovedPlannedCost.ShouldBe(150.00m);
        summary.TotalSpent.ShouldBe(0m);
    }
}
