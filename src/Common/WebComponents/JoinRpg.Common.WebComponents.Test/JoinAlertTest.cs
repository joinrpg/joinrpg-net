namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Иконка у <see cref="JoinAlert"/> необязательна: без неё вывод прежний, с ней — раскладка
/// <c>.join.alert</c> «иконка + текст».
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

        var alert = cut.Find("div.alert");
        alert.ClassList.ShouldContain("alert-info");
        alert.ClassList.ShouldNotContain("join");
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

        var alert = cut.Find("div.alert");
        alert.ClassList.ShouldContain("join");
        alert.ClassList.ShouldContain("alert-info");
        alert.Children.Length.ShouldBe(2, "Иконка и обёртка текста — два соседних элемента");
        alert.Children[1].TextContent.ShouldBe("Текст");
    }
}
