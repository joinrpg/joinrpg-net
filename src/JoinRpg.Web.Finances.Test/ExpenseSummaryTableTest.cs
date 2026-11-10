using Bunit;
using JoinRpg.Web.Finances.Expenses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpenseSummaryTableTest : BunitContext
{
    private sealed class StubExpensesUriLocator : IExpensesUriLocator
    {
        public Uri GetExpensesUri(ProjectIdentification projectId) =>
            new($"/{projectId.Value}/money/expenses", UriKind.Relative);
    }

    public ExpenseSummaryTableTest() => Services.AddSingleton<IExpensesUriLocator>(new StubExpensesUriLocator());

    [Fact]
    public void RendersRowsAndTotals()
    {
        var cut = Render<ExpenseSummaryTable>(p => p
            .Add(x => x.Rows,
            [
                new ExpenseSummaryRowViewModel("Аренда базы\\Жилые корпуса", ApprovedPlannedCost: 3000, ApprovedActualCost: 2800, ProposedCost: 500),
                new ExpenseSummaryRowViewModel("Расходники", ApprovedPlannedCost: 1000, ApprovedActualCost: 0, ProposedCost: 0),
                new ExpenseSummaryRowViewModel("Итого", ApprovedPlannedCost: 4000, ApprovedActualCost: 2800, ProposedCost: 500, IsTotal: true),
            ])
            .Add(x => x.TotalSpent, 2800)
            .Add(x => x.MastersTotal, 7000)
            .Add(x => x.ProjectId, 7));

        cut.Markup.ShouldContain("Расходы");
        cut.Markup.ShouldContain("/7/money/expenses");
        cut.Markup.ShouldContain("(подробнее)");
        cut.Markup.ShouldContain("Аренда базы\\Жилые корпуса");
        cut.Markup.ShouldContain("План (одобрено)");
        cut.Markup.ShouldContain("Факт (одобрено)");
        cut.Markup.ShouldContain("На рассмотрении");
        cut.Markup.ShouldContain("Потрачено (одобрено)");
        cut.Markup.ShouldContain("Осталось у мастеров");
        // «Потрачено» — с минусом
        cut.Find("p").TextContent.ShouldContain("-2800");

        // Цены по порядку: строки (план/факт/на рассмотрении), строка «Итого», потом итоги (потрачено, осталось).
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["3000", "2800", "500", "1000", "0", "0", "4000", "2800", "500", "2800", "7000"]);

        // Строка «Итого» целиком жирная.
        var totalRow = cut.FindAll("tr").Single(tr => tr.TextContent.Contains("Итого"));
        totalRow.QuerySelectorAll("b").Length.ShouldBe(4);
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
            .Add(x => x.MastersTotal, 1500.50m)
            .Add(x => x.ProjectId, 7));

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
            .Add(x => x.MastersTotal, 0)
            .Add(x => x.ProjectId, 7));

        cut.Markup.ShouldContain("Расходов пока нет");
        cut.Markup.ShouldContain("Потрачено (одобрено)");
        cut.Markup.ShouldContain("Осталось у мастеров");
    }
}
