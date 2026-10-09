using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>Подпись кнопки — разметка, а не строка: например, дата из EventTime.</summary>
public class JoinButtonChildContentTest
{
    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    [Fact]
    public void ChildContent_ReplacesPresetLabel()
    {
        using var ctx = CreateContext();
        var cut = ctx.Render<JoinButton>(p => p
            .Add(x => x.Preset, ButtonPreset.Edit)
            .Add(x => x.Link, "/x")
            .AddChildContent("<b>02.03.2026</b>"));

        var link = cut.Find("a");
        link.QuerySelector("b")!.TextContent.ShouldBe("02.03.2026");
        link.TextContent.ShouldNotContain("Изменить");
    }
}
