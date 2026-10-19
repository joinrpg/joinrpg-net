namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Контракт <see cref="SupportInfoPanel"/>: необязательные данные без значения не выводятся,
/// а версия с коммитом читается как текст для копирования в техподдержку.
/// </summary>
public class SupportInfoPanelTest
{
    [Fact]
    public void AllData_IsShown_WithCommitLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SupportInfoPanel>(p => p
            .Add(x => x.ActivityId, "activity-1")
            .Add(x => x.RequestId, "request-1")
            .Add(x => x.Path, "https://joinrpg.ru/1/roles")
            .Add(x => x.Referer, new Uri("https://joinrpg.ru/"))
            .Add(x => x.PodName, "joinrpg-portal-7c85f4cb58-b7b74")
            .Add(x => x.Version, "1.2.3")
            .Add(x => x.CommitSha, "abc123")
            .Add(x => x.CommitUrl, new Uri("https://github.com/joinrpg/joinrpg-net/commit/abc123")));

        var text = cut.Find("div.panel-body").TextContent;
        text.ShouldContain("activity-1");
        text.ShouldContain("joinrpg-portal-7c85f4cb58-b7b74");
        text.ShouldContain("1.2.3 (коммит abc123)");
        cut.FindAll("a").Select(a => a.GetAttribute("href")).ShouldBe(
        [
            "https://joinrpg.ru/1/roles",
            "https://joinrpg.ru/",
            "https://github.com/joinrpg/joinrpg-net/commit/abc123",
        ]);
    }

    [Fact]
    public void OptionalData_IsOmitted()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<SupportInfoPanel>(p => p
            .Add(x => x.RequestId, "request-1")
            .Add(x => x.PodName, "joinrpg-portal-7c85f4cb58-b7b74")
            .Add(x => x.Version, "неизвестна"));

        var text = cut.Find("div.panel-body").TextContent;
        text.ShouldNotContain("ActivityId");
        text.ShouldNotContain("Путь");
        text.ShouldNotContain("Referer");
        text.ShouldNotContain("коммит");
        cut.FindAll("a").ShouldBeEmpty();
    }
}
