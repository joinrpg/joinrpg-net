namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Контракт <see cref="SiteBanner"/>: пустой текст в конфиге означает «баннера нет», а не пустую плашку.
/// </summary>
public class SiteBannerTest
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyText_RendersNothing(string? text)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SiteBanner>(p => p.Add(x => x.Text, text));

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void Text_IsEscaped_NotRenderedAsHtml()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SiteBanner>(p => p.Add(x => x.Text, "<b>Данные</b> восстановлены"));

        cut.FindAll("b").ShouldBeEmpty();
        cut.Find("div.alert").TextContent.Trim().ShouldBe("<b>Данные</b> восстановлены");
    }

    [Theory]
    [InlineData("Подробности: https://joinrpg.ru/drp.", "https://joinrpg.ru/drp")]
    [InlineData("Подробности (http://joinrpg.ru/drp) тут", "http://joinrpg.ru/drp")]
    [InlineData("https://joinrpg.ru/a?b=1 — инструкция", "https://joinrpg.ru/a?b=1")]
    public void Url_BecomesLink_WithoutSurroundingPunctuation(string text, string expectedHref)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SiteBanner>(p => p.Add(x => x.Text, text));

        var link = cut.Find("div.alert a");
        link.GetAttribute("href").ShouldBe(expectedHref);
        link.TextContent.ShouldBe(expectedHref);
        cut.Find("div.alert").TextContent.Trim().ShouldBe(text);
    }

    [Fact]
    public void SeveralUrls_BecomeSeveralLinks()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SiteBanner>(p => p.Add(x => x.Text, "См. https://a.joinrpg.ru/x и https://b.joinrpg.ru/y"));

        cut.FindAll("div.alert a").Select(a => a.TextContent).ShouldBe(["https://a.joinrpg.ru/x", "https://b.joinrpg.ru/y"]);
    }

    [Theory]
    [InlineData("Сайт joinrpg.ru работает")]
    [InlineData("javascript:alert(1) не ссылка")]
    [InlineData("Ссылка https:// оборвана")]
    public void NonHttpUrl_StaysText(string text)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SiteBanner>(p => p.Add(x => x.Text, text));

        cut.FindAll("div.alert a").ShouldBeEmpty();
        cut.Find("div.alert").TextContent.Trim().ShouldBe(text);
    }
}
