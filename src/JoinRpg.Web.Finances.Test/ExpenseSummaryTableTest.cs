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
            .Add(x => x.ProjectId, 7));

        cut.Markup.ShouldContain("Расходы");
        // Ссылкой являются только «подробнее», скобки — вне ссылки
        cut.Markup.ShouldContain("""(<a href="/7/money/expenses">подробнее</a>)""");
        cut.Markup.ShouldContain("Аренда базы\\Жилые корпуса");
        cut.Markup.ShouldContain("План (одобрено)");
        cut.Markup.ShouldContain("Факт (одобрено)");
        cut.Markup.ShouldContain("На рассмотрении");

        // Цены по порядку: строки (план/факт/на рассмотрении), потом строка «Итого».
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["3000", "2800", "500", "1000", "0", "0", "4000", "2800", "500"]);

        // Денежные колонки выравнены по правому краю.
        cut.FindAll("th.text-right").Count.ShouldBe(3);
        cut.FindAll("td.text-right").Count.ShouldBe(9);

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
            .Add(x => x.ProjectId, 7));

        // Копейки показываются только если есть
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["49,9", "149,7", "0"]);
    }

    [Fact]
    public void EmptyRowsShowPlaceholder()
    {
        var cut = Render<ExpenseSummaryTable>(p => p
            .Add(x => x.Rows, [])
            .Add(x => x.ProjectId, 7));

        cut.Markup.ShouldContain("Расходов пока нет");
    }
}
