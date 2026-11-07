using Bunit;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class MyExpensesPanelTest : BunitContext
{
    [Fact]
    public void RendersTotalsAndRows()
    {
        var cut = Render<MyExpensesPanel>(p => p.Add(x => x.Expenses,
        [
            new ExpenseViewModel(
                new ExpenseIdentification(new ProjectIdentification(1), 1),
                "Аренда зала",
                "Аренда базы",
                PlannedQuantity: 2,
                PlannedPrice: 1500,
                ActualQuantity: null,
                ActualPrice: null,
                SpentAt: null,
                State: ExpenseStateView.Approved,
                MasterName: "Иван Мастеров",
                CreatedByName: "Иван Мастеров",
                Description: "Оплата по счёту"),
            new ExpenseViewModel(
                new ExpenseIdentification(new ProjectIdentification(1), 2),
                "Канцелярия",
                "Расходники",
                PlannedQuantity: 10,
                PlannedPrice: 100,
                ActualQuantity: null,
                ActualPrice: null,
                SpentAt: null,
                State: ExpenseStateView.Proposed,
                MasterName: "Иван Мастеров",
                CreatedByName: "Иван Мастеров"),
            new ExpenseViewModel(
                new ExpenseIdentification(new ProjectIdentification(1), 3),
                "Лишний реквизит",
                "Реквизит",
                PlannedQuantity: 5,
                PlannedPrice: 1000,
                ActualQuantity: null,
                ActualPrice: null,
                SpentAt: null,
                State: ExpenseStateView.Rejected,
                MasterName: "Иван Мастеров",
                CreatedByName: "Иван Мастеров"),
        ]));

        cut.Markup.ShouldContain("Мои расходы");
        // 2×1500 + 10×100 = 4000; Rejected (5000) не учитывается ни в одной сумме.
        cut.Markup.ShouldContain("Запланировано:");
        cut.Markup.ShouldContain("<b>4000 ₽</b>");
        cut.Markup.ShouldContain("Одобрено:");
        cut.Markup.ShouldContain("<b>3000 ₽</b>");
        cut.Markup.ShouldContain("Аренда зала");
    }

    [Fact]
    public void EmptyListRendersZeroTotals()
    {
        var cut = Render<MyExpensesPanel>(p => p.Add(x => x.Expenses, []));

        cut.Markup.ShouldContain("Запланировано:");
        cut.Markup.ShouldContain("<b>0 ₽</b>");
        cut.Markup.ShouldContain("Одобрено:");
        cut.Markup.ShouldContain("Расходов пока нет");
    }
}
