using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

/// <summary>
/// <see cref="TypedSelectListItem{TKey}.Disabled"/> должен превращаться в атрибут
/// <c>disabled</c> у <c>&lt;option&gt;</c>: иначе параметр молча игнорируется,
/// хотя второй потребитель той же записи (<see cref="TypedDropdownButton{TKey, TItem}"/>) его учитывает.
/// </summary>
public class TypedSelectorTest
{
    private record Item(int Id, string Label, bool Disabled = false);

    private static readonly Item[] Items =
    [
        new(1, "Первый"),
        new(2, "Второй", Disabled: true),
    ];

    private static BunitContext CreateContext()
    {
        var ctx = new BunitContext();
        // Non-interactive: TypedSelector не пытается импортировать JS-модуль bootstrap-select.
        ctx.SetRendererInfo(new RendererInfo("Static", isInteractive: false));
        return ctx;
    }

    private static IRenderedComponent<TypedSelector<int, Item>> Render(BunitContext ctx)
        => ctx.Render<TypedSelector<int, Item>>(p => p
            .Add(x => x.Items, Items)
            .Add(x => x.KeySelector, (Item item) => item.Id)
            .Add(x => x.ToListItem, (Item item) => TypedSelectList.CreateItem(item.Id, item.Label, Disabled: item.Disabled)));

    [Fact]
    public void DisabledItem_RendersDisabledOption()
    {
        using var ctx = CreateContext();

        var options = Render(ctx).FindAll("option");

        options.Count.ShouldBe(2);
        options[1].TextContent.ShouldBe("Второй");
        options[1].HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void EnabledItem_RendersOptionWithoutDisabledAttribute()
    {
        using var ctx = CreateContext();

        var options = Render(ctx).FindAll("option");

        options[0].TextContent.ShouldBe("Первый");
        options[0].HasAttribute("disabled").ShouldBeFalse();
    }

    private static BunitJSModuleInterop SetupInteractive(BunitContext ctx)
    {
        ctx.SetRendererInfo(new RendererInfo("WebAssembly", isInteractive: true));
        var module = ctx.JSInterop.SetupModule("/_content/JoinRpg.Common.WebComponents/component-interop.js");
        _ = module.SetupVoid("initBootstrapSelect", _ => true);
        _ = module.SetupVoid("refreshBootstrapSelect", _ => true);
        return module;
    }

    /// <summary>
    /// Родитель при каждой своей перерисовке передаёт новые лямбды и массивы. Если пункты от этого не поменялись,
    /// дорогой selectpicker('refresh') звать не нужно (#5250).
    /// </summary>
    [Fact]
    public void ParentRerenderWithSameContent_DoesNotRefresh()
    {
        using var ctx = new BunitContext();
        var module = SetupInteractive(ctx);
        var cut = Render(ctx);
        cut.WaitForAssertion(() => module.Invocations["initBootstrapSelect"].ShouldHaveSingleItem());

        cut.Render(p => p
            .Add(x => x.Items, [.. Items])
            .Add(x => x.KeySelector, (Item item) => item.Id)
            .Add(x => x.ToListItem, (Item item) => TypedSelectList.CreateItem(item.Id, item.Label, Disabled: item.Disabled))
            .Add(x => x.SelectedValues, []));

        module.Invocations["refreshBootstrapSelect"].ShouldBeEmpty();
        module.Invocations["initBootstrapSelect"].ShouldHaveSingleItem();
    }

    [Fact]
    public void SelectedValuesChanged_Refreshes()
    {
        using var ctx = new BunitContext();
        var module = SetupInteractive(ctx);
        var cut = Render(ctx);
        cut.WaitForAssertion(() => module.Invocations["initBootstrapSelect"].ShouldHaveSingleItem());

        cut.Render(p => p.Add(x => x.SelectedValues, [1]));

        module.Invocations["refreshBootstrapSelect"].ShouldHaveSingleItem();
    }
}
