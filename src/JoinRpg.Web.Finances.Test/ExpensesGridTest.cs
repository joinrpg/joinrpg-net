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
        cut.Markup.ShouldContain("2 × 1500 = 3000 ₽");
        cut.Markup.ShouldContain("2 × 1400 = 2800 ₽");
        cut.Markup.ShouldContain("Одобрен");
        cut.Markup.ShouldContain("На рассмотрении");
        cut.Markup.ShouldContain("10 × 100 = 1000 ₽");
        cut.Markup.ShouldContain("15.02.2026");
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

        cut.Markup.ShouldNotContain("Мастер");
        cut.Markup.ShouldNotContain("Иван Мастеров");
        cut.Markup.ShouldContain("Аренда зала");
        cut.Markup.ShouldContain("2 × 1500 = 3000 ₽");
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
    ];

    private sealed class FakeUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) => new($"https://example.org/user/{target.UserId?.Value}");
    }
}
