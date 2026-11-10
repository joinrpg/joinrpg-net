using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

/// <summary>
/// Границы цен в форме расхода: положительные, не более двух знаков (иначе SQL Server округлит
/// decimal(18,2) молча) и влезающие в decimal(18,2) (иначе arithmetic overflow в БД).
/// </summary>
public class ExpenseEditViewModelValidationTest
{
    private static ExpenseEditViewModel ValidForm() => new()
    {
        Name = "Аренда базы",
        CategoryPath = "Аренда базы",
        MasterId = new(1),
        PlannedQuantity = 2,
        PlannedPrice = 15000m,
    };

    private static string[] Validate(ExpenseEditViewModel model)
        => [.. model.Validate(new ValidationContext(model)).SelectMany(r => r.MemberNames)];

    [Fact]
    public void ValidFormWithKopecksPasses()
    {
        var form = ValidForm();
        form.PlannedPrice = 49.90m;
        form.ActualPrice = 14000.50m;
        form.ActualQuantity = 1;
        Validate(form).ShouldBeEmpty();
    }

    [Fact]
    public void SubKopeckPlannedPriceFails()
    {
        var form = ValidForm();
        form.PlannedPrice = 0.001m;
        Validate(form).ShouldContain(nameof(ExpenseEditViewModel.PlannedPrice));
    }

    [Fact]
    public void PlannedPriceBeyondDecimal182Fails()
    {
        var form = ValidForm();
        form.PlannedPrice = 10000000000000000m;
        Validate(form).ShouldContain(nameof(ExpenseEditViewModel.PlannedPrice));
    }

    [Fact]
    public void SubKopeckActualPriceFails()
    {
        var form = ValidForm();
        form.ActualPrice = 0.001m;
        Validate(form).ShouldContain(nameof(ExpenseEditViewModel.ActualPrice));
    }
}
