using JoinRpg.Common.WebComponents.Badges;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Контракт <see cref="PrintableBadge"/> со скриптом подгонки шрифта (printable-badge.js): без скрипта строка
/// печатается максимальным шрифтом, пределы подгонки скрипт читает из data-атрибутов строки, а интерактивный
/// бейдж (превью в редакторе) сам вызывает подгонку после отрисовки.
/// </summary>
public class PrintableBadgeTest
{
    private const string ModulePath = "/_content/JoinRpg.Common.WebComponents/printable-badge.js";

    private static readonly PrintableBadgeLine geralt = new("Геральт", OffsetMm: 18, MinFontPt: 12, MaxFontPt: 32, Bold: true);

    private static BunitContext StaticContext()
    {
        var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    [Fact]
    public void Line_StartsAtMaxFontAndCarriesLimitsForScript()
    {
        using var ctx = StaticContext();
        var cut = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.Lines, [geralt]));

        var line = cut.Find(".join-printable-badge-line");
        line.TextContent.ShouldBe("Геральт");
        line.GetAttribute("style").ShouldNotBeNull().ShouldContain("font-size: 32pt");
        line.GetAttribute("data-min-font-pt").ShouldBe("12");
        line.GetAttribute("data-max-font-pt").ShouldBe("32");
    }

    // Строка у нижнего края прибита к низу — перенос на вторую строку растёт вверх, а не уезжает за край.
    [Fact]
    public void BottomLine_IsPositionedFromBottom()
    {
        using var ctx = StaticContext();
        var cut = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.Lines, [geralt with { OffsetMm = 5, FromBottom = true }]));

        var style = cut.Find(".join-printable-badge-line").GetAttribute("style").ShouldNotBeNull();
        style.ShouldContain("bottom: 5mm");
        style.ShouldNotContain("top:");
    }

    // Фоновые картинки (background-image) браузер по умолчанию не печатает, поэтому фон — <img>.
    [Fact]
    public void Background_IsImageElement()
    {
        using var ctx = StaticContext();
        var cut = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.BackgroundUrl, "https://example.com/bg.png")
            .Add(x => x.Lines, []));

        cut.Find("img.join-printable-badge-background").GetAttribute("src").ShouldBe("https://example.com/bg.png");
    }

    [Fact]
    public void WithoutBackground_NoImage()
    {
        using var ctx = StaticContext();
        var cut = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.Lines, []));

        cut.FindAll("img").ShouldBeEmpty();
    }

    // Превью перерисовывается при каждой правке шаблона — и каждый раз шрифт надо подогнать заново.
    [Fact]
    public void Interactive_FitsAfterEveryRender()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var module = ctx.JSInterop.SetupModule(ModulePath);
        module.SetupVoid("fitBadges", _ => true);

        var cut = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.Lines, [geralt]));
        cut.Render(p => p.Add(x => x.Lines, [geralt with { Text = "Цирилла Фиона Элен Рианнон" }]));

        module.VerifyInvoke("fitBadges", calledTimes: 2);
    }

    [Fact]
    public void Static_DoesNotUseJsInterop()
    {
        using var ctx = StaticContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Strict;

        _ = ctx.Render<PrintableBadge>(p => p
            .Add(x => x.WidthMm, 90)
            .Add(x => x.HeightMm, 55)
            .Add(x => x.Lines, [geralt]));

        ctx.JSInterop.Invocations.ShouldBeEmpty();
    }
}
