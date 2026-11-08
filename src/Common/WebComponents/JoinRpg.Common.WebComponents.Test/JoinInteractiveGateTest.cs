using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// Содержимое WASM-острова с вводом до загрузки WebAssembly не рендерится вовсе: пререндеренная
/// разметка выглядит живой, но Blazor еще не исполняется, и ввод при гидратации острова тихо стирается.
/// Поэтому в статическом рендере — заглушка, в интерактивном — содержимое.
/// </summary>
public class JoinInteractiveGateTest
{
    private const string SecretContent = "секретная форма";

    [Fact]
    public void StaticRender_ShowsLoadingInsteadOfContent()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = ctx.Render<JoinInteractiveGate>(p => p.AddChildContent($"<p>{SecretContent}</p>"));

        cut.Markup.ShouldNotContain(SecretContent);
        cut.Markup.ShouldContain("Идет загрузка");
    }

    [Fact]
    public void StaticRender_UsesCustomMessage()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));

        var cut = ctx.Render<JoinInteractiveGate>(p => p
            .Add(x => x.Message, "Загружаем форму...")
            .AddChildContent($"<p>{SecretContent}</p>"));

        cut.Markup.ShouldContain("Загружаем форму...");
        cut.Markup.ShouldNotContain(SecretContent);
    }

    [Fact]
    public void InteractiveRender_ShowsContent()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));

        var cut = ctx.Render<JoinInteractiveGate>(p => p.AddChildContent($"<p>{SecretContent}</p>"));

        cut.Markup.ShouldContain(SecretContent);
        cut.Markup.ShouldNotContain("Идет загрузка");
    }

    /// <summary>Гейт смотрит только на интерактивность, а не на имя рендерера: Blazor Server тоже показывает содержимое.</summary>
    [Fact]
    public void InteractiveServerRender_ShowsContent()
    {
        using var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo("Server", isInteractive: true));

        var cut = ctx.Render<JoinInteractiveGate>(p => p.AddChildContent($"<p>{SecretContent}</p>"));

        cut.Markup.ShouldContain(SecretContent);
    }
}
