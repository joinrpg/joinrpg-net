using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace JoinRpg.Common.WebComponents.Test;

public class TypedDropdownButtonTest
{
    private record Item(int Id, string Label);

    private static readonly Item[] Items =
    [
        new(1, "Первый"),
        new(2, "Второй"),
    ];

    private static TypedSelectListItem<int> ToListItem(Item item) => TypedSelectList.CreateItem(item.Id, item.Label);

    [Fact]
    public void ToLinkMode_RendersPlainLinksForEachItem()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.ToLink, (Item item) => $"/item/{item.Id}"));

        cut.Markup.ShouldContain("Действия");
        var links = cut.FindAll("a").Where(a => a.GetAttribute("href")!.StartsWith("/item/")).ToList();
        links.Count.ShouldBe(2);
        links[0].GetAttribute("href").ShouldBe("/item/1");
        links[1].GetAttribute("href").ShouldBe("/item/2");
    }

    [Fact]
    public void EmptyItems_RendersNothing()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, [])
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.ToLink, (Item item) => $"/item/{item.Id}"));

        cut.Markup.Trim().ShouldBeEmpty();
    }

    [Fact]
    public void NullItems_RendersLoadingMessage()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, (Item[]?)null)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.ToLink, (Item item) => $"/item/{item.Id}"));

        cut.Markup.ShouldContain("Идет загрузка");
    }

    [Fact]
    public void OnClickMode_WithoutAutoConfirm_ClickInvokesCallbackWithSelectedItem()
    {
        using var ctx = new BunitContext();
        Item? clicked = null;
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.OnClick, (Item item) => clicked = item));

        cut.FindAll("a")[1].Click();

        clicked.ShouldBe(Items[1]);
    }

    [Fact]
    public void SettingBothToLinkAndOnClick_Throws()
    {
        using var ctx = new BunitContext();
        Should.Throw<InvalidOperationException>(() => ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.ToLink, (Item item) => $"/item/{item.Id}")
            .Add(x => x.OnClick, (Item item) => { })));
    }

    [Fact]
    public void AutoConfirmWithToLink_Throws()
    {
        using var ctx = new BunitContext();
        Should.Throw<InvalidOperationException>(() => ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.ToLink, (Item item) => $"/item/{item.Id}")
            .Add(x => x.AutoConfirm, true)
            .Add(x => x.AutoConfirmMessage, "Удалить {name}?")));
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
        Item? clicked = null;
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.OnClick, (Item item) => clicked = item)
            .Add(x => x.AutoConfirm, true)
            .Add(x => x.AutoConfirmMessage, "Удалить {name}?"));

        cut.FindAll("a")[0].Click();

        var cancelButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Отменить"));
        await cancelButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        clicked.ShouldBeNull();
    }

    [Fact]
    public async Task AutoConfirm_UserConfirms_InvokesOnClickWithSelectedItemAndSubstitutesName()
    {
        using var ctx = CreateContextWithDialogSupport();
        Item? clicked = null;
        var cut = ctx.Render<TypedDropdownButton<int, Item>>(p => p
            .Add(x => x.Label, "Действия")
            .Add(x => x.Items, Items)
            .Add(x => x.ToListItem, ToListItem)
            .Add(x => x.OnClick, (Item item) => clicked = item)
            .Add(x => x.AutoConfirm, true)
            .Add(x => x.AutoConfirmMessage, "Удалить {name}?"));

        cut.FindAll("a")[0].Click();

        cut.Markup.ShouldContain("Удалить Первый?");

        var yesButton = cut.FindAll("button").Single(b => b.TextContent.Contains("Да"));
        await yesButton.ClickAsync(new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
        await cut.Find("dialog").TriggerEventAsync("onclose", EventArgs.Empty);

        clicked.ShouldBe(Items[0]);
    }
}
