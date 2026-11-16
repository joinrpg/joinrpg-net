namespace JoinRpg.Common.WebComponents.Test;

public class JoinMenuItemTest
{
    /// <summary>
    /// Между иконкой и подписью пункта меню — пробел, а не склейка «иконкаПункт»: статическую
    /// разметку из одних пробелов (включая <text> </text>) Blazor вырезает (#5324).
    /// </summary>
    [Fact]
    public void IconIsSeparatedFromLabelWithSpace()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Href, "/go")
            .Add(x => x.Label, "Пункт")
            .Add(x => x.Icon, JoinIconType.User));

        cut.Find("a").InnerHtml.ShouldContain("</svg> Пункт");
    }

    /// <summary>Без иконки подпись идёт с начала ссылки — без ведущего пробела.</summary>
    [Fact]
    public void WithoutIcon_LabelStartsImmediately()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Href, "/go")
            .Add(x => x.Label, "Пункт"));

        cut.Find("a").InnerHtml.Trim().ShouldBe("Пункт");
    }
}
