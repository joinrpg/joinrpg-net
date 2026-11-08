using System.ComponentModel.DataAnnotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.Test.Finances;

public class ExpenseValidationTest
{
    private static Expense ValidPlannedExpense() => new()
    {
        Name = "Аренда базы",
        PlannedQuantity = 2,
        PlannedPrice = 15000,
    };

    private static string[] Validate(Expense expense)
        => [.. expense.Validate(new ValidationContext(expense))
            .SelectMany(r => r.MemberNames)];

    [Fact]
    public void ValidPlannedExpensePasses() => Validate(ValidPlannedExpense()).ShouldBeEmpty();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyNameFails(string name)
    {
        var expense = ValidPlannedExpense();
        expense.Name = name;
        Validate(expense).ShouldContain(nameof(Expense.Name));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositivePlannedQuantityFails(int quantity)
    {
        var expense = ValidPlannedExpense();
        expense.PlannedQuantity = quantity;
        Validate(expense).ShouldContain(nameof(Expense.PlannedQuantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositivePlannedPriceFails(int price)
    {
        var expense = ValidPlannedExpense();
        expense.PlannedPrice = price;
        Validate(expense).ShouldContain(nameof(Expense.PlannedPrice));
    }

    [Fact]
    public void ActualPriceWithoutQuantityFails()
    {
        var expense = ValidPlannedExpense();
        expense.ActualPrice = 1000;
        Validate(expense).ShouldContain(nameof(Expense.ActualPrice));
    }

    [Fact]
    public void ActualQuantityWithoutPriceFails()
    {
        var expense = ValidPlannedExpense();
        expense.ActualQuantity = 1;
        Validate(expense).ShouldContain(nameof(Expense.ActualQuantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveActualQuantityFails(int quantity)
    {
        var expense = ValidPlannedExpense();
        expense.ActualQuantity = quantity;
        expense.ActualPrice = 1000;
        Validate(expense).ShouldContain(nameof(Expense.ActualQuantity));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void NonPositiveActualPriceFails(int price)
    {
        var expense = ValidPlannedExpense();
        expense.ActualQuantity = 1;
        expense.ActualPrice = price;
        Validate(expense).ShouldContain(nameof(Expense.ActualPrice));
    }

    [Fact]
    public void ValidActualPasses()
    {
        var expense = ValidPlannedExpense();
        expense.ActualQuantity = 2;
        expense.ActualPrice = 14000;
        expense.SpentAt = new DateTime(2026, 10, 1);
        Validate(expense).ShouldBeEmpty();
    }

    [Fact]
    public void ActualWithoutSpentAtPasses()
    {
        var expense = ValidPlannedExpense();
        expense.ActualQuantity = 2;
        expense.ActualPrice = 14000;
        Validate(expense).ShouldBeEmpty();
    }

    [Fact]
    public void SpentAtWithoutActualPasses()
    {
        // Дата — единое опциональное поле: может быть указана и у планового расхода (без факта)
        var expense = ValidPlannedExpense();
        expense.SpentAt = new DateTime(2026, 10, 1);
        Validate(expense).ShouldBeEmpty();
    }
}
