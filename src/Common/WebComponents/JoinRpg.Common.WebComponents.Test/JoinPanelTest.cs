namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// JoinPanel оформляется собственными стилями (JoinPanel.razor.css) вместо классов
/// Bootstrap 3 (panel, panel-heading, panel-body, panel-footer, panel-*). Тест проверяет
/// разметку: классы панели и её частей и то, что классов Bootstrap в ней не осталось.
/// </summary>
public class JoinPanelTest
{
    private static IRenderedComponent<JoinPanel> Render(
        BunitContext ctx,
        Action<ComponentParameterCollectionBuilder<JoinPanel>>? configure = null)
        => ctx.Render<JoinPanel>(p =>
        {
            p.Add(x => x.Header, "<b>заголовок</b>");
            p.Add(x => x.Body, "<p>содержимое</p>");
            p.Add(x => x.Footer, "<i>футер</i>");
            configure?.Invoke(p);
        });

    [Theory]
    [InlineData(null, "join-panel join-panel--default")]
    [InlineData(VariationStyleEnum.None, "join-panel join-panel--default")]
    [InlineData(VariationStyleEnum.Primary, "join-panel join-panel--primary")]
    [InlineData(VariationStyleEnum.Success, "join-panel join-panel--success")]
    [InlineData(VariationStyleEnum.Warning, "join-panel join-panel--warning")]
    [InlineData(VariationStyleEnum.Danger, "join-panel join-panel--danger")]
    [InlineData(VariationStyleEnum.Info, "join-panel join-panel--info")]
    public void RendersOwnClasses(VariationStyleEnum? variation, string expected)
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, p => p.Add(x => x.Variation, variation));

        cut.Find(".join-panel").GetAttribute("class").ShouldBe(expected);
        cut.Find(".join-panel__heading").TextContent.ShouldBe("заголовок");
        cut.Find(".join-panel__body").TextContent.ShouldBe("содержимое");
        cut.Find(".join-panel__footer").TextContent.ShouldBe("футер");
    }

    [Fact]
    public void Markup_DoesNotUseBootstrap3Classes()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, p => p.Add(x => x.Variation, VariationStyleEnum.Warning));

        cut.Markup.ShouldNotContain("panel-heading");
        cut.Markup.ShouldNotContain("panel-body");
        cut.Markup.ShouldNotContain("panel-footer");
        cut.Markup.ShouldNotContain("panel-default");
        cut.Markup.ShouldNotContain("panel-warning");
    }

    [Fact]
    public void ChildContent_UsedWhenBodyIsNotSet()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinPanel>(p => p
            .Add(x => x.ChildContent, "<p>содержимое</p>"));

        cut.Find(".join-panel__body").TextContent.ShouldBe("содержимое");
    }

    [Fact]
    public void Body_WinsOverChildContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinPanel>(p => p
            .Add(x => x.Body, "<p>тело</p>")
            .Add(x => x.ChildContent, "<p>содержимое</p>"));

        cut.FindAll(".join-panel__body").ShouldHaveSingleItem().TextContent.ShouldBe("тело");
    }

    [Fact]
    public void HeaderAndFooter_OmittedWhenNotSet()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinPanel>(p => p
            .Add(x => x.Body, "<p>содержимое</p>"));

        cut.FindAll(".join-panel__heading").ShouldBeEmpty();
        cut.FindAll(".join-panel__footer").ShouldBeEmpty();
    }
}
