using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Строка-заглушка <see cref="JoinBorderedTable{TItem}.EmptyContent"/> показывается, только когда
/// строк нет.
/// </summary>
public class JoinBorderedTableTest
{
    // Текст прямо в <tr> без <td> HTML-парсер выносит за пределы таблицы — поэтому ячейка.
    private static readonly RenderFragment<string> Row = item => builder =>
    {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", "item");
        builder.OpenElement(2, "td");
        builder.AddContent(3, item);
        builder.CloseElement();
        builder.CloseElement();
    };

    private static readonly RenderFragment Empty = builder =>
    {
        builder.OpenElement(0, "tr");
        builder.AddAttribute(1, "class", "empty");
        builder.OpenElement(2, "td");
        builder.AddContent(3, "(пусто)");
        builder.CloseElement();
        builder.CloseElement();
    };

    [Fact]
    public void NoItems_ShowsEmptyContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinBorderedTable<string>>(p => p
            .Add(x => x.Items, [])
            .Add(x => x.ItemTemplate, Row)
            .Add(x => x.EmptyContent, Empty));

        cut.Find("tbody tr.empty").TextContent.ShouldBe("(пусто)");
    }

    [Fact]
    public void WithItems_HidesEmptyContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinBorderedTable<string>>(p => p
            .Add(x => x.Items, ["101", "102"])
            .Add(x => x.ItemTemplate, Row)
            .Add(x => x.EmptyContent, Empty));

        cut.FindAll("tbody tr.item").Count.ShouldBe(2);
        cut.FindAll("tbody tr.empty").ShouldBeEmpty();
    }

    [Fact]
    public void NoItemsWithoutEmptyContent_RendersEmptyBody()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinBorderedTable<string>>(p => p
            .Add(x => x.Items, [])
            .Add(x => x.ItemTemplate, Row));

        cut.FindAll("tbody tr").ShouldBeEmpty();
    }
}
