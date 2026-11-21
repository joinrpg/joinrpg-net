using System.Globalization;
using Bunit;
using JoinRpg.Blazor.Client.CommonPages;
using Shouldly;
using Xunit;

namespace JoinRpg.Blazor.Client.Test;

public class BudgetWidgetTest : BunitContext
{
    private static readonly CultureInfo Ru = CultureInfo.GetCultureInfo("ru-RU");

    private static readonly BudgetWidgetModel Model = new(
        Months:
        [
            new BudgetPeriodCard("Октябрь 2026", Income: 18_400m, Expense: 31_250m), // итог −12 850
            new BudgetPeriodCard("Сентябрь 2026", Income: 42_100m, Expense: 29_800m), // итог +12 300
        ],
        Years:
        [
            new BudgetPeriodCard("С начала 2026", Income: 247_300m, Expense: 312_400m), // итог −65 100
            new BudgetPeriodCard("2025", Income: 396_800m, Expense: 371_200m), // итог +25 600
        ]);

    [Fact]
    public void RendersHeaderPeriodsAndSums()
    {
        var cut = Render<BudgetWidget>(p => p.Add(x => x.Model, Model));

        cut.Markup.ShouldContain("Бюджет Joinrpg");
        cut.Markup.ShouldContain("Октябрь 2026");
        cut.Markup.ShouldContain("Сентябрь 2026");
        cut.Markup.ShouldContain("С начала 2026");
        cut.Markup.ShouldContain("2025");
        cut.Markup.ShouldContain("Доход");
        cut.Markup.ShouldContain("Расход");
        cut.Markup.ShouldContain("Итог");

        var october = Model.Months[0];
        october.Total.ShouldBeLessThan(0); // итог отрицательный
        cut.Markup.ShouldContain($"{october.Income.ToString("#,0.##", Ru)} ₽");
        cut.Markup.ShouldContain($"{october.Expense.ToString("#,0.##", Ru)} ₽");
        // отрицательный итог помечен минусом
        cut.Markup.ShouldContain($"{october.Total.ToString("#,0.##", Ru)} ₽");
        cut.Markup.ShouldContain("-" + october.Total.ToString("#,0.##", Ru).TrimStart('-') + " ₽");

        var year2025 = Model.Years[1];
        year2025.Total.ShouldBeGreaterThan(0); // положительный итог без минуса
        cut.Markup.ShouldContain($"{year2025.Total.ToString("#,0.##", Ru)} ₽");
    }

    [Fact]
    public void NullModelRendersNothing()
    {
        var cut = Render<BudgetWidget>(p => p.Add(x => x.Model, (BudgetWidgetModel?)null));

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void EmptyModelRendersNothing()
    {
        var cut = Render<BudgetWidget>(p => p.Add(x => x.Model, new BudgetWidgetModel([], [])));

        cut.Markup.Trim().ShouldBeEmpty();
    }
}
