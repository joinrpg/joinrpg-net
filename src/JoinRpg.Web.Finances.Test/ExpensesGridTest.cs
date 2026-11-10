using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Finances;
using JoinRpg.Web.Finances.Expenses;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class ExpensesGridTest : BunitContext
{
    public ExpensesGridTest()
        => Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new FakeUserLinkLocator());

    [Fact]
    public void RendersMasterAsUserLink()
    {
        var cut = RenderGrid();

        var link = cut.Find("a.join-user");
        link.TextContent.ShouldContain("Иван Мастеров");
        link.GetAttribute("href").ShouldBe("https://example.org/user/15");
    }

    [Fact]
    public void RendersExpenseRows()
    {
        var cut = RenderGrid();

        cut.Markup.ShouldContain("Аренда базы\\Жилые корпуса");
        cut.Markup.ShouldContain("Аренда зала");
        cut.Markup.ShouldContain("Иван Мастеров");
        // ₽ — псевдоэлемент PriceElement (CSS), в разметке только числа
        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("2 × 1400 = 2800");
        cut.Markup.ShouldContain("Ранее планировалось 2 × 1500 ₽ = 3000 ₽");
        cut.Markup.ShouldContain("Потрачен");
        cut.Markup.ShouldContain("Запрошен");
        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("10 × 100 = 1000");
        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("3 × 200 = 600");
        cut.Markup.ShouldContain("15.02.2026");
    }

    [Fact]
    public void SingleCostColumnHeader()
    {
        var cut = RenderGrid();

        var headers = cut.FindAll("th").Select(header => header.TextContent.Trim());
        headers.ShouldContain("Стоимость");
        headers.ShouldNotContain("План");
        headers.ShouldNotContain("Факт");
    }

    [Fact]
    public void MatchingPlanAndFactShowsSingleValueWithoutTooltip()
    {
        var cut = RenderGrid();

        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("3 × 200 = 600");
        cut.Markup.ShouldNotContain("Ранее планировалось 3 × 200");
    }

    [Fact]
    public void DifferentPlanAndFactShowsActualWithPlanTooltip()
    {
        var cut = RenderGrid();

        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("2 × 1400 = 2800");
        cut.Markup.ShouldContain("Ранее планировалось 2 × 1500 ₽ = 3000 ₽");
        // use href символа иконки Info
        cut.Markup.ShouldContain("info-circle");
    }

    [Fact]
    public void RejectedExpenseStruckThrough()
    {
        var cut = RenderGrid();

        // 3 ячейки (категория, название, стоимость) × 2 отклонённых расхода
        cut.FindAll(".expense-rejected-strike").Count.ShouldBe(6);
    }

    [Fact]
    public void NonRejectedExpensesNotStruckThrough()
    {
        var cut = RenderGrid();

        var struckText = string.Join(" ", cut.FindAll(".expense-rejected-strike").Select(span => span.TextContent));
        struckText.ShouldContain("Отклонённая аренда");
        struckText.ShouldContain("Отклонённые расходники");
        struckText.ShouldNotContain("Аренда зала");
        struckText.ShouldNotContain("Канцелярия");
        struckText.ShouldNotContain("Транспорт");
    }

    [Fact]
    public void SpentAtColumnHeaderIsDate()
    {
        var cut = RenderGrid();

        cut.Markup.ShouldNotContain("Дата факта");
        cut.FindAll("th").ShouldContain(header => header.TextContent.Trim() == "Дата");
    }

    [Fact]
    public void EmptyListShowsPlaceholder()
    {
        var cut = Render<ExpensesGrid>(p => p.Add(x => x.Expenses, []));

        cut.Markup.ShouldContain("Расходов пока нет");
    }

    [Fact]
    public void WithoutRowActionsNoActionsColumn()
    {
        var cut = RenderGrid();

        cut.Markup.ShouldNotContain("Действия");
    }

    [Fact]
    public void ShowMasterFalseHidesMasterColumn()
    {
        var cut = Render<ExpensesGrid>(p => p
            .Add(x => x.Expenses, SampleExpenses)
            .Add(x => x.ShowMaster, false));

        cut.FindAll("th").ShouldNotContain(header => header.TextContent.Trim() == "Мастер");
        cut.Markup.ShouldNotContain("Иван Мастеров");
        cut.Markup.ShouldContain("Аренда зала");
        cut.FindAll(".expense-cost > span").Select(e => e.TextContent.Trim()).ShouldContain("2 × 1400 = 2800");
    }

    [Fact]
    public void RowActionsRenderedForEachRow()
    {
        var cut = Render<ExpensesGrid>(p => p
            .Add(x => x.Expenses, SampleExpenses)
            .Add(x => x.RowActions, expense => builder => builder.AddContent(0, $"править:{expense.Name}")));

        cut.Markup.ShouldContain("Действия");
        cut.Markup.ShouldContain("править:Аренда зала");
        cut.Markup.ShouldContain("править:Канцелярия");
    }

    private IRenderedComponent<ExpensesGrid> RenderGrid()
        => Render<ExpensesGrid>(p => p.Add(x => x.Expenses, SampleExpenses));

    private static readonly ExpenseViewModel[] SampleExpenses =
    [
        new(
            new ExpenseIdentification(new ProjectIdentification(1), 1),
            "Аренда зала",
            "Аренда базы\\Жилые корпуса",
            PlannedQuantity: 2,
            PlannedPrice: 1500,
            ActualQuantity: 2,
            ActualPrice: 1400,
            SpentAt: new DateOnly(2026, 2, 15),
            State: ExpenseStateView.Approved,
            MasterName: "Иван Мастеров",
            CreatedByName: "Пётр Создателев",
            MasterId: new UserIdentification(15)),
        new(
            new ExpenseIdentification(new ProjectIdentification(1), 2),
            "Канцелярия",
            "Оргвзнос\\Расходники",
            PlannedQuantity: 10,
            PlannedPrice: 100,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseStateView.Proposed,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
        new(
            new ExpenseIdentification(new ProjectIdentification(1), 3),
            "Транспорт",
            "Оргвзнос\\Логистика",
            PlannedQuantity: 3,
            PlannedPrice: 200,
            ActualQuantity: 3,
            ActualPrice: 200,
            SpentAt: null,
            State: ExpenseStateView.Approved,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
        new(
            new ExpenseIdentification(new ProjectIdentification(1), 4),
            "Отклонённая аренда",
            "Аренда базы\\Поле",
            PlannedQuantity: 1,
            PlannedPrice: 5000,
            ActualQuantity: 1,
            ActualPrice: 5200,
            SpentAt: null,
            State: ExpenseStateView.Rejected,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
        new(
            new ExpenseIdentification(new ProjectIdentification(1), 5),
            "Отклонённые расходники",
            "Оргвзнос\\Расходники",
            PlannedQuantity: 5,
            PlannedPrice: 80,
            ActualQuantity: null,
            ActualPrice: null,
            SpentAt: null,
            State: ExpenseStateView.Rejected,
            MasterName: "Иван Мастеров",
            CreatedByName: "Иван Мастеров"),
    ];

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.org/user/{target.UserId?.Value}");
    }
}
