namespace JoinRpg.Common.WebComponents.Test;

public class JoinPagerTest
{
    private static IRenderedComponent<JoinPager> Render(BunitContext ctx, int page, int totalPages, Action<int>? onChange = null)
        => ctx.Render<JoinPager>(p => p
            .Add(x => x.Page, page)
            .Add(x => x.TotalPages, totalPages)
            .Add(x => x.PageChanged, onChange ?? (_ => { })));

    private static IReadOnlyList<string> Items(IRenderedComponent<JoinPager> cut)
        => [.. cut.FindAll("li").Select(li => li.TextContent.Trim())];

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void SinglePage_RendersNothing(int totalPages)
    {
        using var ctx = new BunitContext();

        Render(ctx, 1, totalPages).Markup.Trim().ShouldBeEmpty();
    }

    [Theory]
    [InlineData(4, 10, new[] { "1", "2", "3", "4", "5", "6", "…", "10" })]
    [InlineData(7, 10, new[] { "1", "…", "5", "6", "7", "8", "9", "10" })]
    [InlineData(6, 12, new[] { "1", "…", "4", "5", "6", "7", "8", "…", "12" })]
    public void MiddlePage_ShowsNeighboursFirstLastAndGaps(int page, int totalPages, string[] numbers)
    {
        using var ctx = new BunitContext();

        Items(Render(ctx, page, totalPages)).ShouldBe(["«", .. numbers, "»"]);
    }

    /// <summary>
    /// Пропуск, который прячет одну страницу, не нужен: сама страница занимает столько же места.
    /// </summary>
    [Fact]
    public void GapOfOnePage_ShowsThePageInstead()
    {
        using var ctx = new BunitContext();

        Items(Render(ctx, 5, 10)).ShouldBe(["«", "1", "2", "3", "4", "5", "6", "7", "…", "10", "»"]);
    }

    [Theory]
    [InlineData(2, new[] { "1", "2", "3", "4", "…", "10" })]
    [InlineData(9, new[] { "1", "…", "7", "8", "9", "10" })]
    [InlineData(10, new[] { "1", "…", "8", "9", "10" })]
    public void NearEdges_NoGapNextToFirstOrLast(int page, string[] numbers)
    {
        using var ctx = new BunitContext();

        Items(Render(ctx, page, 10)).ShouldBe(["«", .. numbers, "»"]);
    }

    [Fact]
    public void TwoPages_BothNumbers()
    {
        using var ctx = new BunitContext();

        Items(Render(ctx, 1, 2)).ShouldBe(["«", "1", "2", "»"]);
    }

    /// <summary>
    /// Номер из старой ссылки бывает больше числа страниц (фильтр сузил список) — рисуем последнюю.
    /// </summary>
    [Fact]
    public void PageBeyondLast_ShownAsLast()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, 20, 5);

        cut.Find("li.active").TextContent.Trim().ShouldBe("5");
        cut.FindAll("a[aria-label='Следующая страница']").ShouldBeEmpty();
        cut.Find("a[aria-label='Предыдущая страница']").ShouldNotBeNull();
    }

    [Fact]
    public void PageBeforeFirst_ShownAsFirst()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, 0, 5);

        cut.Find("li.active").TextContent.Trim().ShouldBe("1");
        cut.FindAll("a[aria-label='Предыдущая страница']").ShouldBeEmpty();
    }

    [Fact]
    public void CurrentPage_IsActiveAndNotALink()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, 3, 5);

        var active = cut.Find("li.active");
        active.TextContent.Trim().ShouldBe("3");
        active.QuerySelector("a").ShouldBeNull();
    }

    [Fact]
    public void FirstPage_PreviousIsDisabled()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, 1, 5);

        cut.FindAll("li")[0].ClassList.ShouldContain("disabled");
        cut.FindAll("li")[0].QuerySelector("a").ShouldBeNull();
    }

    [Fact]
    public void LastPage_NextIsDisabled()
    {
        using var ctx = new BunitContext();
        var cut = Render(ctx, 5, 5);

        cut.FindAll("li")[^1].ClassList.ShouldContain("disabled");
        cut.FindAll("li")[^1].QuerySelector("a").ShouldBeNull();
    }

    [Fact]
    public void ClickNext_RaisesPageChanged()
    {
        using var ctx = new BunitContext();
        int? changed = null;
        var cut = Render(ctx, 2, 5, page => changed = page);

        cut.Find("a[aria-label='Следующая страница']").Click();

        changed.ShouldBe(3);
    }

    [Fact]
    public void ClickNumber_RaisesPageChanged()
    {
        using var ctx = new BunitContext();
        int? changed = null;
        var cut = Render(ctx, 2, 5, page => changed = page);

        cut.Find("a[aria-label='Страница 5']").Click();

        changed.ShouldBe(5);
    }
}
