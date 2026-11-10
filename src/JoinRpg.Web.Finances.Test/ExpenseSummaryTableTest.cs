using Bunit;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseSummaryTableTest : BunitContext
{
    [Fact]
    public void RendersRowsAndTotals()
    {
        var cut = Render<ExpenseSummaryTable>(p => p
            .Add(x => x.Rows,
            [
                new ExpenseSummaryRowViewModel("Аренда базы\\Жилые корпуса", ApprovedPlannedCost: 3000, ApprovedActualCost: 2800, ProposedCost: 500),
                new ExpenseSummaryRowViewModel("Расходники", ApprovedPlannedCost: 1000, ApprovedActualCost: 0, ProposedCost: 0),
            ])
            .Add(x => x.TotalSpent, 2800)
            .Add(x => x.MastersTotal, 7000));

        cut.Markup.ShouldContain("Расходы");
        cut.Markup.ShouldContain("Аренда базы\\Жилые корпуса");
        cut.Markup.ShouldContain("План (одобрено)");
        cut.Markup.ShouldContain("Факт (одобрено)");
        cut.Markup.ShouldContain("На рассмотрении");
        cut.Markup.ShouldContain("Потрачено (одобрено)");
        cut.Markup.ShouldContain("Осталось у мастеров");
        // «Потрачено» — с минусом
        cut.Find("p").TextContent.ShouldContain("-2800");

        // Цены по порядку: строки (план/факт/на рассмотрении), потом итоги (потрачено, осталось).
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["3000", "2800", "500", "1000", "0", "0", "2800", "7000"]);
    }

    [Fact]
    public void RendersKopecksWhenPresent()
    {
        var cut = Render<ExpenseSummaryTable>(p => p
            .Add(x => x.Rows,
            [
                new ExpenseSummaryRowViewModel("Расходники", ApprovedPlannedCost: 49.90m, ApprovedActualCost: 149.70m, ProposedCost: 0),
            ])
            .Add(x => x.TotalSpent, 149.70m)
            .Add(x => x.MastersTotal, 1500.50m));

        // Копейки показываются только если есть — и в строках, и в итогах
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["49,9", "149,7", "0", "149,7", "1500,5"]);
        cut.Find("p").TextContent.ShouldContain("-149,7");
    }

    [Fact]
    public void EmptyRowsShowPlaceholderButTotalsStay()
    {
        var cut = Render<ExpenseSummaryTable>(p => p
            .Add(x => x.Rows, [])
            .Add(x => x.TotalSpent, 0)
            .Add(x => x.MastersTotal, 0));

        cut.Markup.ShouldContain("Расходов пока нет");
        cut.Markup.ShouldContain("Потрачено (одобрено)");
        cut.Markup.ShouldContain("Осталось у мастеров");
    }
}
