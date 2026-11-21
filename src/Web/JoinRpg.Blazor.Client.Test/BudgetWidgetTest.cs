using Bunit;
using JoinRpg.Blazor.Client.CommonPages;
using Shouldly;
using Xunit;

namespace JoinRpg.Blazor.Client.Test;

public class BudgetWidgetTest : BunitContext
{
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

        // суммы захардкожены (неразрывный пробел U+00A0 — разделитель тысяч ru-RU):
        // дублирование формата компонента в тесте не ловит баги форматирования
        cut.Markup.ShouldContain("18\u00A0400 ₽");  // доход «Октябрь 2026»
        cut.Markup.ShouldContain("31\u00A0250 ₽");  // расход «Октябрь 2026»
        cut.Markup.ShouldContain("-12\u00A0850 ₽"); // итог «Октябрь 2026» — отрицательный, ASCII-минус
        cut.Markup.ShouldContain("25\u00A0600 ₽");  // итог «2025» — положительный
        cut.Markup.ShouldNotContain("-25\u00A0600 ₽");
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
