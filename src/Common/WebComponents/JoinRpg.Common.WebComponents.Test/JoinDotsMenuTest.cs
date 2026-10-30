namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Меню JoinDotsMenu открывается браузером (нативный <c>&lt;details&gt;</c>), поэтому JS Bootstrap
/// ему не нужен — как и JoinDropdownButton (#5975).
/// </summary>
public class JoinDotsMenuTest
{
    [Fact]
    public void RendersNativeDetailsMenu_WithIconToggle()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinDotsMenu>(p => p
            .AddChildContent("<li><a href=\"/go\">пункт</a></li>"));

        var details = cut.Find("details.join-dropdown");
        var summary = cut.Find("details.join-dropdown > summary");
        summary.GetAttribute("aria-label").ShouldBe("действия");
        summary.QuerySelector("svg").ShouldNotBeNull(); // иконка «три точки»
        cut.Find("details.join-dropdown > ul > li > a").GetAttribute("href").ShouldBe("/go");
    }
}
