using AngleSharp.Dom;
using Microsoft.AspNetCore.Components;

namespace JoinRpg.Common.WebComponents.Test;

public class TypedSelectorTest
{
    private record Item(int Id, string Label, string Subtext = "", string ExtraSearch = "", bool Disabled = false);

    private static readonly Item[] TwoItems =
    [
        new(1, "Первый"),
        new(2, "Второй", Disabled: true),
    ];

    /// <summary>Больше 10 пунктов — появляется поиск.</summary>
    private static readonly Item[] ManyItems =
    [
        new(1, "Алёна", Subtext: "Северный клан"),
        new(2, "Йорген", ExtraSearch: "кузнец"),
        new(3, "Борис"),
        .. Enumerable.Range(4, 9).Select(i => new Item(i, $"Персонаж {i}")),
    ];

    private static BunitContext CreateContext(bool interactive = true)
    {
        var ctx = new BunitContext();
        ctx.SetRendererInfo(new RendererInfo(interactive ? "WebAssembly" : "Static", isInteractive: interactive));
        return ctx;
    }

    private static IRenderedComponent<TypedSelector<int, Item>> Render(
        BunitContext ctx,
        Item[]? items = null,
        Action<ComponentParameterCollectionBuilder<TypedSelector<int, Item>>>? configure = null)
        => ctx.Render<TypedSelector<int, Item>>(p =>
        {
            _ = p
                .Add(x => x.Items, items ?? TwoItems)
                .Add(x => x.KeySelector, (Item item) => item.Id)
                .Add(x => x.ToListItem, (Item item) => TypedSelectList.CreateItem(
                    item.Id, item.Label, Subtext: item.Subtext, ExtraSearch: item.ExtraSearch, Disabled: item.Disabled));
            configure?.Invoke(p);
        });

    private static IElement Input(IRenderedComponent<TypedSelector<int, Item>> cut, string value)
        => cut.Find($".join-select__list input[value='{value}']");

    private static string Summary(IRenderedComponent<TypedSelector<int, Item>> cut)
        => cut.Find(".join-select__value").TextContent;

    private static string[] VisibleTexts(IRenderedComponent<TypedSelector<int, Item>> cut)
        => [.. cut.FindAll(".join-select__list > li").Where(li => !li.HasAttribute("hidden")).Select(li => li.QuerySelector(".join-select__text")!.TextContent)];

    [Fact]
    public void ItemsNotLoaded_ShowsLoadingInsteadOfList()
    {
        using var ctx = CreateContext();

        var cut = ctx.Render<TypedSelector<int, Item>>(p => p
            .Add(x => x.Items, null)
            .Add(x => x.KeySelector, (Item item) => item.Id)
            .Add(x => x.ToListItem, (Item item) => TypedSelectList.CreateItem(item.Id, item.Label)));

        cut.FindAll("details.join-select").ShouldBeEmpty();
        cut.Markup.ShouldContain("Идет загрузка");
    }

    /// <summary>
    /// Форма постит value пунктов как есть: это полный идентификатор, его разбирает ProjectEntityIdModelBinder.
    /// </summary>
    [Fact]
    public void SingleSelect_RendersRadiosWithNameAndKeyAsValue()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p.Add(x => x.Name, "TargetId"));

        var inputs = cut.FindAll(".join-select__list input");
        inputs.Select(i => i.GetAttribute("type")).ShouldAllBe(type => type == "radio");
        inputs.Select(i => i.GetAttribute("name")).ShouldAllBe(name => name == "TargetId");
        inputs.Select(i => i.GetAttribute("value")).ShouldBe(["1", "2"]);
    }

    [Fact]
    public void MultipleSelect_RendersCheckboxes()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p.Add(x => x.Multiple, true));

        cut.FindAll(".join-select__list input").Select(i => i.GetAttribute("type")).ShouldAllBe(type => type == "checkbox");
    }

    /// <summary>Без Name radio всё равно группируются, иначе в браузере отметились бы сразу несколько.</summary>
    [Fact]
    public void WithoutName_RadiosShareGeneratedName()
    {
        using var ctx = CreateContext();

        var names = Render(ctx).FindAll(".join-select__list input").Select(i => i.GetAttribute("name")).Distinct().ToArray();

        names.ShouldHaveSingleItem().ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void DisabledItem_RendersDisabledInput()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx);

        Input(cut, "1").HasAttribute("disabled").ShouldBeFalse();
        Input(cut, "2").HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public void SelectedValues_AreCheckedAndShownOnToggle()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p.Add(x => x.SelectedValues, [2]));

        Input(cut, "2").HasAttribute("checked").ShouldBeTrue();
        Input(cut, "1").HasAttribute("checked").ShouldBeFalse();
        Summary(cut).ShouldBe("Второй");
    }

    /// <summary>
    /// Нативный &lt;select&gt; без пустого пункта сам выбирал первый пункт: на кнопке было видно одно, а в модели
    /// интерактивной формы — пусто, статическая же форма постила то, чего человек не выбирал.
    /// </summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SingleWithoutEmptyAndNothingSelected_NothingChecked(bool interactive)
    {
        using var ctx = CreateContext(interactive);

        var cut = Render(ctx, [new(1, "Первый"), new(2, "Второй")]);

        cut.FindAll(".join-select__list input[checked]").ShouldBeEmpty();
        Summary(cut).ShouldBe("(нет)");
    }

    [Fact]
    public void AllowEmpty_RendersCheckedEmptyRadioWithEmptyText()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.EmptyText, "(не выбрано)"));

        var empty = Input(cut, "");
        empty.HasAttribute("checked").ShouldBeTrue();
        Input(cut, "1").HasAttribute("checked").ShouldBeFalse();
        Summary(cut).ShouldBe("(не выбрано)");
    }

    /// <summary>У множественного выбора «пусто» — это ничего не отмечено, пустой пункт там лишний.</summary>
    [Fact]
    public void MultipleWithAllowEmpty_DoesNotRenderEmptyItem()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.AllowEmpty, true));

        cut.FindAll(".join-select__list input[value='']").ShouldBeEmpty();
        Summary(cut).ShouldBe("(нет)");
    }

    [Fact]
    public void PickRadio_RaisesSelectedValuesAndItems()
    {
        using var ctx = CreateContext();
        int[]? values = null;
        Item[]? items = null;
        Item[] list = [new(1, "Первый"), new(2, "Второй")];
        var cut = Render(ctx, list, p => p
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.SelectedValuesChanged, (int[] v) => values = v)
            .Add(x => x.SelectedItemsChanged, (Item[] i) => items = i));

        Input(cut, "2").Change(true);

        values.ShouldBe([2]);
        items.ShouldBe([list[1]]);
        Summary(cut).ShouldBe("Второй");
    }

    [Fact]
    public void PickEmpty_RaisesEmptySelection()
    {
        using var ctx = CreateContext();
        int[]? values = null;
        var cut = Render(ctx, configure: p => p
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.SelectedValues, [1])
            .Add(x => x.SelectedValuesChanged, (int[] v) => values = v));

        Input(cut, "").Change(true);

        values.ShouldBe([]);
    }

    /// <summary>
    /// Обёртки вроде CharacterSelector слушают только SelectedItemsChanged и не возвращают выбор в SelectedValues.
    /// Второй клик не должен терять первый.
    /// </summary>
    [Fact]
    public void Multiple_ParentDoesNotFeedBack_KeepsEarlierChecks()
    {
        using var ctx = CreateContext();
        int[]? values = null;
        Item[] list = [new(1, "Первый"), new(2, "Второй"), new(3, "Третий")];
        var cut = Render(ctx, list, p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.SelectedValuesChanged, (int[] v) => values = v));

        Input(cut, "3").Change(true);
        Input(cut, "1").Change(true);

        values.ShouldBe([1, 3]);
        Summary(cut).ShouldBe("Первый, Третий");

        Input(cut, "3").Change(false);

        values.ShouldBe([1]);
    }

    [Fact]
    public void ParentPassesNewSelectedValues_Applied()
    {
        using var ctx = CreateContext();
        var cut = Render(ctx, configure: p => p.Add(x => x.SelectedValues, [1]));

        cut.Render(p => p.Add(x => x.SelectedValues, [2]));

        Input(cut, "2").HasAttribute("checked").ShouldBeTrue();
        Input(cut, "1").HasAttribute("checked").ShouldBeFalse();
    }

    /// <summary>Родитель принял выбор, а потом сам вернул прежнее значение (например, отменил правку).</summary>
    [Fact]
    public void ParentAcceptsThenResetsSelection_Applied()
    {
        using var ctx = CreateContext();
        Item[] list = [new(1, "Первый"), new(2, "Второй")];
        var cut = Render(ctx, list, p => p.Add(x => x.SelectedValues, [1]));

        Input(cut, "2").Change(true);
        cut.Render(p => p.Add(x => x.SelectedValues, [2]));
        cut.Render(p => p.Add(x => x.SelectedValues, [1]));

        Input(cut, "1").HasAttribute("checked").ShouldBeTrue();
        Input(cut, "2").HasAttribute("checked").ShouldBeFalse();
        Summary(cut).ShouldBe("Первый");
    }

    /// <summary>
    /// Выбранного пункта больше нет в списке: на кнопке «пусто», и форма должна постить пустое значение,
    /// а не ничего — как прежний &lt;select&gt; с пустым пунктом.
    /// </summary>
    [Fact]
    public void SelectedKeyMissingFromItems_EmptyItemChecked()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, [new(1, "Первый")], p => p
            .Add(x => x.AllowEmpty, true)
            .Add(x => x.SelectedValues, [42]));

        Input(cut, "").HasAttribute("checked").ShouldBeTrue();
        Summary(cut).ShouldBe("(нет)");
    }

    [Fact]
    public void ItemsReplaced_SelectionOfRemainingItemKept()
    {
        using var ctx = CreateContext();
        var cut = Render(ctx, [new(1, "Первый"), new(2, "Второй")], p => p.Add(x => x.SelectedValues, [2]));

        cut.Render(p => p.Add(x => x.Items, [new(2, "Второй"), new(3, "Третий")]));

        Input(cut, "2").HasAttribute("checked").ShouldBeTrue();
        Summary(cut).ShouldBe("Второй");
    }

    [Fact]
    public void FewItems_NoSearch()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx);

        cut.FindAll(".join-select__search").ShouldBeEmpty();
        cut.FindAll("[data-search]").ShouldBeEmpty();
    }

    [Theory]
    [InlineData("алена", "Алёна")] // ё как е
    [InlineData("СЕВЕРНЫЙ", "Алёна")] // по подсказке, без учёта регистра
    [InlineData("кузнец", "Йорген")] // по дополнительным словам
    [InlineData("йорг", "Йорген")]
    [InlineData("персонаж 12", "Персонаж 12")] // каждое слово запроса
    public void Search_FiltersByTextSubtextAndExtraSearch(string query, string expected)
    {
        using var ctx = CreateContext();
        var cut = Render(ctx, ManyItems);

        cut.Find(".join-select__search").Input(query);

        VisibleTexts(cut).ShouldBe([expected]);
    }

    /// <summary>Отфильтрованные пункты остаются в разметке — выбранный не выпадает из формы.</summary>
    [Fact]
    public void Search_HidesButKeepsSelectedInForm()
    {
        using var ctx = CreateContext();
        var cut = Render(ctx, ManyItems, p => p.Add(x => x.SelectedValues, [3]));

        cut.Find(".join-select__search").Input("алена");

        var selectedItem = Input(cut, "3");
        selectedItem.HasAttribute("checked").ShouldBeTrue();
        selectedItem.Closest("li")!.HasAttribute("hidden").ShouldBeTrue();
    }

    [Fact]
    public void Search_EmptyQuery_ShowsAll()
    {
        using var ctx = CreateContext();
        var cut = Render(ctx, ManyItems);

        cut.Find(".join-select__search").Input("алена");
        cut.Find(".join-select__search").Input("");

        VisibleTexts(cut).Length.ShouldBe(ManyItems.Length);
    }

    /// <summary>
    /// В статическом рендере фильтрует скрипт в браузере: ему нужны нормализованные строки пунктов и пометка корня.
    /// </summary>
    [Fact]
    public void Static_MarksRootAndRendersNormalizedSearchStrings()
    {
        using var ctx = CreateContext(interactive: false);

        var cut = Render(ctx, ManyItems, p => p.Add(x => x.Name, "CharacterId"));

        cut.Find("details.join-select").HasAttribute("data-join-select-static").ShouldBeTrue();
        Input(cut, "1").Closest("li")!.GetAttribute("data-search").ShouldBe("алена северныи клан");
    }

    [Fact]
    public void Interactive_RootNotMarkedStatic()
    {
        using var ctx = CreateContext();

        Render(ctx).Find("details.join-select").HasAttribute("data-join-select-static").ShouldBeFalse();
    }

    [Fact]
    public void Width_SetsInlineWidth()
    {
        using var ctx = CreateContext();

        var cut = Render(ctx, configure: p => p.Add(x => x.Width, "30em"));

        cut.Find("details.join-select").GetAttribute("style").ShouldBe("width: 30em");
    }
}
