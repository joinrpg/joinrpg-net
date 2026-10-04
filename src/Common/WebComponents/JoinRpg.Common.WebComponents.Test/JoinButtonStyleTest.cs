using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// JoinButton рисуется собственными классами и не зависит от классов Bootstrap 3:
/// иначе правила BS для родителей (.btn-group &gt; .btn и т. п.) снова начнут действовать на него частично.
/// </summary>
public class JoinButtonStyleTest
{
    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    [Theory]
    [InlineData(null, null, "join-btn join-btn--default")]
    [InlineData(VariationStyleEnum.None, SizeStyleEnum.Medium, "join-btn join-btn--default")]
    [InlineData(VariationStyleEnum.Primary, SizeStyleEnum.Large, "join-btn join-btn--primary join-btn--lg")]
    [InlineData(VariationStyleEnum.Success, SizeStyleEnum.Small, "join-btn join-btn--success join-btn--sm")]
    [InlineData(VariationStyleEnum.Info, SizeStyleEnum.ExtraSmall, "join-btn join-btn--info join-btn--xs")]
    [InlineData(VariationStyleEnum.Warning, null, "join-btn join-btn--warning")]
    [InlineData(VariationStyleEnum.Danger, null, "join-btn join-btn--danger")]
    public void RendersOwnClasses(VariationStyleEnum? style, SizeStyleEnum? size, string expected)
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Label, "Кнопка")
            .Add(x => x.Style, style)
            .Add(x => x.Size, size));

        cut.Find("button").GetAttribute("class").ShouldBe(expected);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData(null, true)]
    [InlineData("/somewhere", false)]
    [InlineData("/somewhere", true)]
    public void RenderedButton_HasNoBootstrapClasses(string? link, bool withTitle)
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Label, "Кнопка")
            .Add(x => x.Preset, ButtonPreset.Delete)
            .Add(x => x.Link, link)
            .Add(x => x.Title, withTitle ? "Подсказка" : null));

        var element = cut.Find(link is null ? "button" : "a");
        element.ClassList.ShouldContain("join-btn");
        element.ClassList.ShouldContain("join-btn--danger");
        element.ClassList.ShouldAllBe(c => c != "btn" && !c.StartsWith("btn-"));
    }
}
