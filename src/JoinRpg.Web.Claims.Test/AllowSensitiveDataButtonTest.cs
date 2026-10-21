using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Web.Claims.Test;

/// <summary>
/// Кнопка «Разрешить доступ к паспортным данным»: раньше ошибку не ловила вовсе, кнопка молча зависала.
/// Причину отказа сервера показывает, сырой текст неожиданной ошибки — нет (#5735).
/// </summary>
public class AllowSensitiveDataButtonTest : BunitContext
{
    private static readonly ProjectIdentification ProjectId = new(123);

    private readonly FakeClaimOperationClient client = new();

    public AllowSensitiveDataButtonTest()
    {
        Services.AddSingleton<IClaimOperationClient>(client);
        Services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        var dialogModule = JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = dialogModule.SetupVoid("showModal", _ => true);
        _ = dialogModule.SetupVoid("closeModal", _ => true);
        SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
    }

    private async Task<IRenderedComponent<AllowSensitiveDataButton>> ClickAndConfirm()
    {
        var cut = Render<AllowSensitiveDataButton>(parameters => parameters.Add(c => c.ProjectId, ProjectId));
        var click = cut.Find("button").ClickAsync(new MouseEventArgs());
        var confirm = cut.FindAll("dialog").Single(d => d.TextContent.Contains("Мастера игры получат доступ"));
        await confirm.QuerySelectorAll("button").Single(b => b.TextContent.Contains("Разрешить доступ")).ClickAsync(new MouseEventArgs());
        await cut.FindAll("dialog").Single(d => d.TextContent.Contains("Мастера игры получат доступ"))
            .TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
        return cut;
    }

    [Fact]
    public async Task RefusalReasonIsShown()
    {
        client.Failure = new ServerProblemException("Проект находится в архиве, изменения в нём невозможны");

        var cut = await ClickAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain(
            "Не удалось разрешить доступ к паспортным данным: Проект находится в архиве, изменения в нём невозможны"));
    }

    [Fact]
    public async Task UnexpectedFailureShowsGenericText()
    {
        client.Failure = new HttpRequestException("Response status code does not indicate success: 502 (Bad Gateway).");

        var cut = await ClickAndConfirm();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Не удалось разрешить доступ к паспортным данным"));
        cut.Markup.ShouldNotContain("Bad Gateway");
    }
}
