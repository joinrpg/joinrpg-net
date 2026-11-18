using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace JoinRpg.Web.Accommodation.Test;

internal static class ConfirmedClick
{
    /// <summary>
    /// Нажимает кнопку вне диалогов и подтверждает открывшийся диалог: кнопка в нём,
    /// затем событие close, которое в браузере шлёт сам &lt;dialog&gt;.
    /// </summary>
    public static async Task ClickAndConfirm<TComponent>(
        this IRenderedComponent<TComponent> cut,
        string buttonLabel,
        string dialogText,
        string confirmLabel)
        where TComponent : IComponent
    {
        var click = cut.FindAll("button")
            .First(b => b.Closest("dialog") is null && b.TextContent.Trim() == buttonLabel)
            .ClickAsync(new MouseEventArgs());

        IElement FindDialog() => cut.FindAll("dialog").Single(d => d.TextContent.Contains(dialogText));

        await FindDialog().QuerySelectorAll("button")
            .Single(b => b.TextContent.Trim() == confirmLabel)
            .ClickAsync(new MouseEventArgs());
        await FindDialog().TriggerEventAsync("onclose", EventArgs.Empty);
        await click;
    }
}
