namespace JoinRpg.Common.WebComponents.Test;

public class JoinIconTest
{
    [Fact]
    public void EveryIconTypeHasDefinition()
    {
        using var ctx = new BunitContext();
        foreach (var icon in Enum.GetValues<JoinIconType>())
        {
            var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, icon));
            cut.Markup.Trim().ShouldNotBeEmpty($"Иконка {icon} не отрисовалась");
        }
    }

    /// <summary>
    /// Способ отрисовки ровно один: svg со ссылкой на символ спрайта.
    /// </summary>
    [Theory]
    [InlineData(JoinIconType.Delete)]
    [InlineData(JoinIconType.MandatoryStar)]
    [InlineData(JoinIconType.Vk)]
    public void AnyIcon_RendersSvgUseOfSprite(JoinIconType icon)
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, icon));

        cut.Markup.ShouldContain("<svg");
        cut.Markup.ShouldContain("join-icon");
        cut.Markup.ShouldContain("<use");
        cut.Markup.ShouldContain(JoinIconMarkup.SpriteUrl);
        cut.Markup.ShouldNotContain("<img");
    }

    [Fact]
    public void Icon_ReferencesItsOwnSymbol()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, JoinIconType.Delete));
        cut.Markup.ShouldContain("#" + JoinIconDefinitions.Get(JoinIconType.Delete).IconName);
    }

    [Fact]
    public void IconWithOwnVariation_RendersItsColor()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, JoinIconType.MandatoryStar));
        cut.Markup.ShouldContain("text-danger");
    }

    [Fact]
    public void WithoutTitle_RendersNoTooltip()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, JoinIconType.Info));
        cut.Markup.ShouldNotContain("join-tooltip");
    }

    [Fact]
    public void WithTitle_RendersTooltip()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p
            .Add(x => x.Icon, JoinIconType.Info)
            .Add(x => x.Title, "Подсказка"));
        cut.Markup.ShouldContain("join-tooltip");
        cut.Markup.ShouldContain("Подсказка");
    }

    [Fact]
    public void IconWithoutOwnVariation_RendersNoColorClass()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, JoinIconType.Delete));
        cut.Markup.ShouldNotContain("text-");
    }

    [Fact]
    public void WithoutSize_RendersNoFontSize()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, JoinIconType.Delete));
        cut.Markup.ShouldNotContain("font-size");
    }

    [Fact]
    public void WithSize_RendersFontSize()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p
            .Add(x => x.Icon, JoinIconType.Delete)
            .Add(x => x.Size, SizeStyleEnum.Large));
        cut.Markup.ShouldContain("font-size");
    }

    [Fact]
    public void ExtraAttributesArePassedThrough()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinIcon>(p => p
            .Add(x => x.Icon, JoinIconType.Delete)
            .AddUnmatched("data-test", "value"));
        cut.Markup.ShouldContain("data-test=\"value\"");
    }

    /// <summary>
    /// Тег-хелпер MVC строит разметку тем же кодом, что и компонент — значит она должна совпадать.
    /// </summary>
    [Fact]
    public void TagHelperMarkup_MatchesComponentMarkup()
    {
        using var ctx = new BunitContext();
        foreach (var icon in Enum.GetValues<JoinIconType>())
        {
            var componentMarkup = ctx.Render<JoinIcon>(p => p.Add(x => x.Icon, icon)).Markup.Trim();
            var tagHelperMarkup = JoinIconMarkup.BuildHtml(icon);
            NormalizeMarkup(tagHelperMarkup).ShouldBe(NormalizeMarkup(componentMarkup), $"Иконка {icon}");
        }
    }

    private static string NormalizeMarkup(string markup)
        => markup.Replace(" />", ">").Replace("/>", ">").Replace("\"", "'").Trim();
}
