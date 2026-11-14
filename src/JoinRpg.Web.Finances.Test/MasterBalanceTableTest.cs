using Bunit;
using JoinRpg.Common.WebComponents;
using JoinRpg.Web.Finances.Money;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Web.Finances.Test;

public class MasterBalanceTableTest : BunitContext
{
    private sealed class StubUserLinkLocator : IUriLocator<UserLinkViewModel>
    {
        public Uri GetUri(UserLinkViewModel target) =>
            new($"https://example.com/user/{target.UserId!.Value}");
    }

    public MasterBalanceTableTest() => Services.AddSingleton<IUriLocator<UserLinkViewModel>>(new StubUserLinkLocator());

    private static MasterBalanceRowViewModel Row(
        int userId, string name,
        decimal total, decimal fee, decimal received, decimal sent, decimal expenses, decimal planned,
        string? detailsLink = null)
        => new(new UserLinkViewModel(new UserIdentification(userId), name, ViewMode.Show),
            total, fee, received, sent, expenses, planned, detailsLink);

    [Fact]
    public void RendersRowsWithDetailsLinks()
    {
        var cut = Render<MasterBalanceTable>(p => p
            .Add(x => x.Rows,
            [
                Row(1, "Вася", total: 700, fee: 1000, received: 200, sent: 0, expenses: -500, planned: -300, detailsLink: "/1/money/bymaster?masterId=1"),
                Row(2, "Марья", total: 300, fee: 500, received: 0, sent: -200, expenses: 0, planned: 0),
            ])
            .Add(x => x.ShowTotal, false));

        cut.Markup.ShouldContain("Мастер");
        cut.Markup.ShouldContain("Денег на руках");
        cut.Markup.ShouldContain("Собрано взносов");
        cut.Markup.ShouldContain("Получено от других мастеров");
        cut.Markup.ShouldContain("Отправлено другим мастерам");
        cut.Markup.ShouldContain("Расходы");
        cut.Markup.ShouldContain("Запланировано");

        cut.Markup.ShouldContain("Вася");
        cut.Markup.ShouldContain("Марья");
        cut.Markup.ShouldContain("/1/money/bymaster?masterId=1");
        cut.Markup.ShouldContain("(подробнее)");

        // У второй строки ссылки нет
        cut.FindAll("a").Count(a => a.TextContent.Contains("(подробнее)")).ShouldBe(1);

        // Таблица с рамками
        cut.Find("table").ClassList.ShouldContain("table-bordered");
    }

    [Fact]
    public void ShowTotalAppendsBoldTotalsRow()
    {
        var cut = Render<MasterBalanceTable>(p => p
            .Add(x => x.Rows,
            [
                Row(1, "Вася", total: 700, fee: 1000, received: 200, sent: 0, expenses: -500, planned: -300),
                Row(2, "Марья", total: 300.50m, fee: 500, received: 0, sent: -200, expenses: 0, planned: 0),
            ])
            .Add(x => x.ShowTotal, true));

        // Цены по порядку: две строки мастеров (по 7 колонок), потом строка «Итого».
        cut.FindAll(".price-value").Select(e => e.TextContent.Trim()).ToArray()
            .ShouldBe(["700", "1000", "200", "0", "-500", "-300", "300,5", "500", "0", "-200", "0", "0",
                       "1000,5", "1500", "200", "-200", "-500", "-300"]);

        var totalRow = cut.FindAll("tr").Single(tr => tr.TextContent.Contains("Итого"));
        // Строка «Итого» выделена классом, жирность задаётся стилем для ячеек.
        totalRow.ClassList.ShouldContain("total");
        totalRow.QuerySelectorAll("b").ShouldBeEmpty();
        totalRow.QuerySelectorAll("a").ShouldBeEmpty();

        // PriceElement — прямой потомок ячеек: обёртка вокруг него ломает
        // scoped-правило td:has(> .price-value), выравнивающее суммы вправо.
        totalRow.QuerySelectorAll("td > .price-value").Count.ShouldBe(6);
    }

    [Fact]
    public void NoTotalRowWhenEmpty()
    {
        var cut = Render<MasterBalanceTable>(p => p
            .Add(x => x.Rows, Array.Empty<MasterBalanceRowViewModel>())
            .Add(x => x.ShowTotal, true));

        cut.Markup.ShouldContain("Движений денег пока нет");
        cut.Markup.ShouldNotContain("Итого");
        cut.FindAll(".price-value").ShouldBeEmpty();
    }
}
