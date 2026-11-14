using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

public class JoinMenuItemTest
{
    [Fact]
    public void Href_RendersPlainLink()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Href, "/go")
            .Add(x => x.Label, "Перейти"));

        var link = cut.Find("a");
        link.GetAttribute("href").ShouldBe("/go");
        link.GetAttribute("role").ShouldBeNull();
    }

    [Fact]
    public void OnClick_RendersButtonLinkAndInvokesCallback()
    {
        using var ctx = new BunitContext();
        var clicked = false;
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить")
            .Add(x => x.OnClick, () => clicked = true));

        var link = cut.Find("a");
        link.GetAttribute("role").ShouldBe("button");
        link.GetAttribute("href").ShouldBeNull();

        link.Click();

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void OnClickMode_IsFocusable()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить")
            .Add(x => x.OnClick, () => { }));

        cut.Find("a").GetAttribute("tabindex").ShouldBe("0");
    }

    [Fact]
    public void OnClickMode_EnterKeyInvokesCallback()
    {
        using var ctx = new BunitContext();
        var clicked = false;
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить")
            .Add(x => x.OnClick, () => clicked = true));

        cut.Find("a").KeyDown(new Microsoft.AspNetCore.Components.Web.KeyboardEventArgs { Key = "Enter" });

        clicked.ShouldBeTrue();
    }

    [Fact]
    public void OnClickAndHref_Throws()
    {
        using var ctx = new BunitContext();
        Should.Throw<InvalidOperationException>(() => ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Href, "/go")
            .Add(x => x.Label, "Удалить")
            .Add(x => x.OnClick, () => { })));
    }

    [Fact]
    public void AutoConfirmWithoutMessage_Throws()
    {
        using var ctx = new BunitContext();
        Should.Throw<InvalidOperationException>(() => ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить")
            .Add(x => x.OnClick, () => { })
            .Add(x => x.AutoConfirm, true)));
    }

    private static BunitContext CreateContextWithDialogSupport()
    {
        var ctx = new BunitContext();
        ctx.Services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(NullLoggerFactory.Instance);
        ctx.Services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(NullLogger<>));

        var joinDialogModule = ctx.JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        joinDialogModule.SetupVoid("showModal", _ => true);
        joinDialogModule.SetupVoid("closeModal", _ => true);

        return ctx;
    }

    [Fact]
    public async Task AutoConfirm_UserCancels_DoesNotInvokeOnClick()
    {
        using var ctx = CreateContextWithDialogSupport();
        var clicked = false;
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить заявку")
            .Add(x => x.OnClick, () => clicked = true)
            .Add(x => x.AutoConfirm, true)
            .Add(x => x.AutoConfirmMessage, "Удалить заявку?"));

        cut.Find("a[role=button]").Click();

        var cancelButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Отменить"));
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        clicked.ShouldBeFalse();
    }

    [Fact]
    public async Task AutoConfirm_UserConfirms_InvokesOnClick()
    {
        using var ctx = CreateContextWithDialogSupport();
        var clicked = false;
        var cut = ctx.Render<JoinMenuItem>(p => p
            .Add(x => x.Label, "Удалить заявку")
            .Add(x => x.OnClick, () => clicked = true)
            .Add(x => x.AutoConfirm, true)
            .Add(x => x.AutoConfirmMessage, "Удалить заявку?"));

        cut.Find("a[role=button]").Click();

        cut.Markup.ShouldContain("Удалить заявку?");

        var confirmButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Удалить заявку"));
        await confirmButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        clicked.ShouldBeTrue();
    }
}
