using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.Impl.Repositories;
using JoinRpg.DataModel;
using JoinRpg.DataModel.Extensions;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Finances;
using Shouldly;
using Xunit;

namespace JoinRpg.Dal.Impl.Test.Finances;

/// <summary>
/// Юнит-тесты чистого маппинга <see cref="ExpenseMapper"/> (ADR030). БД не используется.
/// </summary>
public class ExpenseMapperTest
{
    private static readonly ProjectIdentification ProjectId = new(1);

    private static readonly IReadOnlyDictionary<int, string> CategoryNames = new Dictionary<int, string>
    {
        [1] = "Аренда базы",
        [2] = "Жилые корпуса",
        [3] = "Антураж",
    };

    [Fact]
    public void PlannedExpenseMapsWithoutActual()
    {
        var expense = new Expense
        {
            ExpenseId = 10,
            ProjectId = ProjectId.Value,
            Name = "Корпус №1",
            Description = new MarkdownDbValue(),
            MasterId = 7,
            CreatedById = 7,
            PlannedQuantity = 2,
            PlannedPrice = 15000,
            State = ExpenseState.Proposed,
            CategoryPathIds = [1, 2],
        };

        var info = ExpenseMapper.Map(expense, CategoryNames);

        info.ExpenseId.ShouldBe(expense.GetId());
        info.Name.ShouldBe("Корпус №1");
        info.PlannedCost.ShouldBe(30000);
        info.ActualCost.ShouldBeNull();
        info.State.ShouldBe(ExpenseState.Proposed);
        info.MasterId.ShouldBe(new UserIdentification(7));
        info.CreatedById.ShouldBe(new UserIdentification(7));
        info.CategoryPath.Select(c => c.Name).ShouldBe(["Аренда базы", "Жилые корпуса"]);
        info.CategoryPath.Select(c => c.CategoryId)
            .ShouldBe([new ExpenseCategoryIdentification(ProjectId, 1), new ExpenseCategoryIdentification(ProjectId, 2)]);
    }

    [Fact]
    public void ActualMapsWithCost()
    {
        var expense = new Expense
        {
            ExpenseId = 11,
            ProjectId = ProjectId.Value,
            Name = "Макеты",
            MasterId = 8,
            CreatedById = 7,
            PlannedQuantity = 5,
            PlannedPrice = 2000,
            ActualQuantity = 4,
            ActualPrice = 1900,
            SpentAt = new DateTime(2026, 10, 1),
            State = ExpenseState.Approved,
            CategoryPathIds = [3],
        };

        var info = ExpenseMapper.Map(expense, CategoryNames);

        info.ActualCost.ShouldBe(7600);
        info.SpentAt.ShouldBe(new DateOnly(2026, 10, 1));
        info.CategoryPath.Single().Name.ShouldBe("Антураж");
    }

    [Fact]
    public void CategoryMapsWithParent()
    {
        var root = new ExpenseCategory
        {
            ExpenseCategoryId = 1,
            ProjectId = ProjectId.Value,
            Name = "Аренда базы",
            ParentCategoryId = null,
            ParentCategoryIds = [],
        };
        var leaf = new ExpenseCategory
        {
            ExpenseCategoryId = 2,
            ProjectId = ProjectId.Value,
            Name = "Жилые корпуса",
            ParentCategoryId = 1,
            ParentCategoryIds = [1],
        };
        var names = new Dictionary<int, string> { [1] = "Аренда базы", [2] = "Жилые корпуса" };

        ExpenseMapper.MapCategory(root, hasActiveDirectExpenses: false, names).ParentCategoryId.ShouldBeNull();
        var mappedLeaf = ExpenseMapper.MapCategory(leaf, hasActiveDirectExpenses: true, names);
        mappedLeaf.ParentCategoryId.ShouldBe(new ExpenseCategoryIdentification(ProjectId, 1));
        mappedLeaf.IsActive.ShouldBeTrue();
        mappedLeaf.HasActiveDirectExpenses.ShouldBeTrue();
        mappedLeaf.FullPath.ShouldBe("Аренда базы\\Жилые корпуса");
    }
}
