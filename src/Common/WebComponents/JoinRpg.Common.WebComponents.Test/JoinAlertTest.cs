namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Иконка у <see cref="JoinAlert"/> необязательна: без неё вывод прежний, с ней — раскладка
/// <c>.join-alert--with-icon</c> «иконка + текст». Классы Bootstrap (alert/alert-*) не выдаются:
/// оформление — в scoped CSS пакета (JoinAlert.razor.css), алерт читается и без Bootstrap.
/// </summary>
public class JoinAlertTest
{
    [Fact]
    public void WithoutIcon_RendersPlainAlert()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinAlert>(p => p
            .Add(x => x.Variation, VariationStyleEnum.Info)
            .AddChildContent("Текст"));

        var alert = cut.Find("div.join-alert");
        alert.ClassList.ShouldContain("join-alert--info");
        cut.FindAll("svg").ShouldBeEmpty();
        alert.TextContent.Trim().ShouldBe("Текст");
    }

    [Fact]
    public void WithIcon_RendersIconBeforeText()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinAlert>(p => p
            .Add(x => x.Variation, VariationStyleEnum.Info)
            .Add(x => x.Icon, JoinIconType.Info)
            .AddChildContent("Текст"));

        var alert = cut.Find("div.join-alert");
        alert.ClassList.ShouldContain("join-alert--with-icon");
        alert.Children.Length.ShouldBe(2, "Иконка и обёртка текста — два соседних элемента");
        alert.Children[1].TextContent.ShouldBe("Текст");
    }

    [Theory]
    [InlineData(null, "join-alert join-alert--default")]
    [InlineData(VariationStyleEnum.None, "join-alert join-alert--default")]
    [InlineData(VariationStyleEnum.Primary, "join-alert join-alert--primary")]
    [InlineData(VariationStyleEnum.Success, "join-alert join-alert--success")]
    [InlineData(VariationStyleEnum.Info, "join-alert join-alert--info")]
    [InlineData(VariationStyleEnum.Warning, "join-alert join-alert--warning")]
    [InlineData(VariationStyleEnum.Danger, "join-alert join-alert--danger")]
    public void RendersOwnClasses(VariationStyleEnum? variation, string expected)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinAlert>(p => p
            .Add(x => x.Variation, variation)
            .AddChildContent("Текст"));

        cut.Find("div").GetAttribute("class").ShouldBe(expected);
    }

    [Fact]
    public void RenderedAlert_HasNoBootstrapClasses()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinAlert>(p => p
            .Add(x => x.Variation, VariationStyleEnum.Info)
            .AddChildContent("Текст"));

        var alert = cut.Find("div");
        alert.ClassList.ShouldAllBe(c => c != "alert" && !c.StartsWith("alert-"));
    }
}
