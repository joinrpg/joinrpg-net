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
        int[]? categoryIds = null,
        params string[] categoryPath)
        => new(
            new ExpenseIdentification(ProjectId, id),
            $"Расход {id}",
            Description: null,
            [.. categoryPath.Select((name, index) => new ExpenseCategoryBriefInfo(
                new ExpenseCategoryIdentification(ProjectId, categoryIds?[index] ?? 100 + index), name))],
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
            Expense(1, ExpenseState.Approved, 2, 100, 2, 90, categoryIds: [100, 101], categoryPath: ["Аренда базы", "Жилые корпуса"]),
            Expense(2, ExpenseState.Proposed, 1, 50, categoryIds: [100, 101], categoryPath: ["Аренда базы", "Жилые корпуса"]),
            Expense(3, ExpenseState.Approved, 3, 10, categoryIds: [300], categoryPath: ["Транспорт"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        var categories = summary.Rows.Where(r => !r.IsTotal).ToArray();
        categories.Length.ShouldBe(3);
        var rent = categories.First(r => r.CategoryPath == "Аренда базы\\Жилые корпуса");
        rent.ApprovedPlannedCost.ShouldBe(200);
        rent.ApprovedActualCost.ShouldBe(180);
        rent.ProposedCost.ShouldBe(50);
        var rentTotal = categories.First(r => r.CategoryPath == "Аренда базы");
        rentTotal.ApprovedPlannedCost.ShouldBe(200);
        rentTotal.ApprovedActualCost.ShouldBe(180);
        rentTotal.ProposedCost.ShouldBe(50);
        var transport = categories.First(r => r.CategoryPath == "Транспорт");
        transport.ApprovedPlannedCost.ShouldBe(30);
        transport.ApprovedActualCost.ShouldBe(0);
        transport.ProposedCost.ShouldBe(0);
        summary.TotalSpent.ShouldBe(180);

        // Итого — сумма по расходам, а не по строкам: агрегация подкатегорий
        // в родителей не должна давать двойной счёт.
        var total = summary.Rows.Last();
        total.IsTotal.ShouldBeTrue();
        total.CategoryPath.ShouldBe("Итого");
        total.ApprovedPlannedCost.ShouldBe(230);
        total.ApprovedActualCost.ShouldBe(180);
        total.ProposedCost.ShouldBe(50);
    }

    [Fact]
    public void Build_ParentCategoryWithoutDirectExpenses_AppearsWithSubtreeSums()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 1, 100, 1, 100, categoryPath: ["Аренда базы", "Жилые корпуса"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        var categories = summary.Rows.Where(r => !r.IsTotal).ToArray();
        categories.Length.ShouldBe(2);
        var rentTotal = categories.First(r => r.CategoryPath == "Аренда базы");
        rentTotal.ApprovedPlannedCost.ShouldBe(100);
        rentTotal.ApprovedActualCost.ShouldBe(100);
        var rent = categories.First(r => r.CategoryPath == "Аренда базы\\Жилые корпуса");
        rent.ApprovedPlannedCost.ShouldBe(100);
        rent.ApprovedActualCost.ShouldBe(100);
        // Итог = 100, а не 200: самый очевидный кандидат на двойной счёт.
        var total = summary.Rows.Last();
        total.IsTotal.ShouldBeTrue();
        total.ApprovedPlannedCost.ShouldBe(100);
        total.ApprovedActualCost.ShouldBe(100);
    }

    [Fact]
    public void Build_RejectedOnlySubtree_DoesNotCreateParentRow()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Rejected, 1, 100, 1, 100, categoryPath: ["Аренда базы", "Жилые корпуса"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Where(r => !r.IsTotal).ShouldBeEmpty();
        summary.TotalSpent.ShouldBe(0);
        // Строка «Итого» показывается всегда, даже когда все суммы нулевые.
        var total = summary.Rows.Single();
        total.IsTotal.ShouldBeTrue();
        total.ApprovedPlannedCost.ShouldBe(0);
        total.ApprovedActualCost.ShouldBe(0);
        total.ProposedCost.ShouldBe(0);
    }

    [Fact]
    public void Build_EmptyCategoryPath_DoesNotCreateRow()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 1, 100, 1, 100, categoryPath: []),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Where(r => !r.IsTotal).ShouldBeEmpty();
        // Расход без категории не даёт строки категории, но входит в «Итого».
        var total = summary.Rows.Single();
        total.IsTotal.ShouldBeTrue();
        total.ApprovedPlannedCost.ShouldBe(100);
        total.ApprovedActualCost.ShouldBe(100);
    }

    [Fact]
    public void Build_ExcludesRejectedFromAllSums()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Rejected, 1, 100, 1, 100, categoryIds: [100], categoryPath: ["Аренда базы"]),
            Expense(2, ExpenseState.Proposed, 2, 20, categoryIds: [300], categoryPath: ["Транспорт"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        var categories = summary.Rows.Where(r => !r.IsTotal).ToArray();
        categories.Length.ShouldBe(1);
        categories.Single().CategoryPath.ShouldBe("Транспорт");
        categories.Single().ProposedCost.ShouldBe(40);
        summary.Rows.Last().ProposedCost.ShouldBe(40);
        summary.TotalSpent.ShouldBe(0);
    }

    [Fact]
    public void Build_SameNameDifferentIds_AreSeparateRows()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 1, 100, categoryIds: [100], categoryPath: ["Прочее"]),
            Expense(2, ExpenseState.Approved, 1, 200, categoryIds: [200], categoryPath: ["Прочее"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        // Категории зовутся одинаково, но id разные — строки должны быть раздельными.
        var categories = summary.Rows.Where(r => !r.IsTotal).ToArray();
        categories.Length.ShouldBe(2);
        categories.Sum(r => r.ApprovedPlannedCost).ShouldBe(300);
        summary.Rows.Last().ApprovedPlannedCost.ShouldBe(300);
    }

    [Fact]
    public void Build_ProposedWithActual_UsesActualCost()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Proposed, 2, 50, 2, 45, categoryPath: ["Аренда базы"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Where(r => !r.IsTotal).Single().ProposedCost.ShouldBe(90);
        summary.Rows.Last().ProposedCost.ShouldBe(90);
    }

    [Fact]
    public void Build_ApprovedWithoutActual_CountsOnlyIntoPlanned()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 3, 10, categoryPath: ["Аренда базы"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        summary.Rows.Where(r => !r.IsTotal).Single().ApprovedPlannedCost.ShouldBe(30);
        summary.Rows.Where(r => !r.IsTotal).Single().ApprovedActualCost.ShouldBe(0);
        summary.TotalSpent.ShouldBe(0);
    }

    [Fact]
    public void Build_SumsKopeckCostsAcrossExpenses()
    {
        ExpenseInfo[] expenses =
        [
            Expense(1, ExpenseState.Approved, 1, 49.90m, categoryIds: [100], categoryPath: ["Аренда базы"]),
            Expense(2, ExpenseState.Approved, 1, 100.10m, categoryIds: [100], categoryPath: ["Аренда базы"]),
        ];

        var summary = ExpenseSummaryBuilder.Build(expenses);

        // 49,90 + 100,10 = 150,00 — копейки не теряются при суммировании
        summary.Rows.Where(r => !r.IsTotal).Single().ApprovedPlannedCost.ShouldBe(150.00m);
        summary.Rows.Last().ApprovedPlannedCost.ShouldBe(150.00m);
        summary.TotalSpent.ShouldBe(0m);
    }
}
