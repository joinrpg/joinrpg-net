namespace JoinRpg.WebComponents.Test;

public class EntitySelectorTest
{
    private static EntitySelectorItem<int>[] CreateItems(int count) =>
        Enumerable.Range(1, count)
            .Select(i => new EntitySelectorItem<int>(Key: i, Text: $"Item {i}", ExtraSearch: $"search{i}"))
            .ToArray();

    [Fact]
    public void SingleSelect_RendersSelectedItem()
    {
        using var ctx = new BunitContext();
        var items = CreateItems(3);

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, [2]);
        });

        var options = cut.FindAll("option");
        options.Count.ShouldBe(3);
        options[1].GetAttribute("selected").ShouldNotBeNull();
        options[0].GetAttribute("selected").ShouldBeNull();
        options[2].GetAttribute("selected").ShouldBeNull();
    }

    [Fact]
    public void NoSelection_NoOptionSelected()
    {
        using var ctx = new BunitContext();
        var items = CreateItems(3);

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
        });

        foreach (var option in cut.FindAll("option"))
        {
            option.GetAttribute("selected").ShouldBeNull();
        }
    }

    [Fact]
    public void SearchInput_NotShownWhenFewItems()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, CreateItems(10));
            parameters.Add(x => x.SelectedKeys, []);
        });

        cut.FindAll("input[type=search]").Count.ShouldBe(0);
    }

    [Fact]
    public void SearchInput_ShownWhenManyItems()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, CreateItems(11));
            parameters.Add(x => x.SelectedKeys, []);
        });

        cut.FindAll("input[type=search]").Count.ShouldBe(1);
    }

    [Fact]
    public void Search_FiltersItemsByText()
    {
        using var ctx = new BunitContext();
        var items = new[]
        {
            new EntitySelectorItem<int>(Key: 1, Text: "Иванов Иван"),
            new EntitySelectorItem<int>(Key: 2, Text: "Петров Пётр"),
            new EntitySelectorItem<int>(Key: 3, Text: "Сидоров Сидор"),
            new EntitySelectorItem<int>(Key: 4, Text: "Иванова Мария"),
            new EntitySelectorItem<int>(Key: 5, Text: "Козлов Андрей"),
            new EntitySelectorItem<int>(Key: 6, Text: "Новиков Олег"),
            new EntitySelectorItem<int>(Key: 7, Text: "Морозов Дмитрий"),
            new EntitySelectorItem<int>(Key: 8, Text: "Волков Алексей"),
            new EntitySelectorItem<int>(Key: 9, Text: "Зайцев Николай"),
            new EntitySelectorItem<int>(Key: 10, Text: "Соколов Игорь"),
            new EntitySelectorItem<int>(Key: 11, Text: "Лебедев Виктор"),
        };

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
        });

        cut.Find("input[type=search]").Input("Иван");

        var options = cut.FindAll("option");
        options.Count.ShouldBe(2);
        options[0].TextContent.ShouldContain("Иванов");
        options[1].TextContent.ShouldContain("Иванова");
    }

    [Fact]
    public void Search_FiltersItemsByExtraSearch()
    {
        using var ctx = new BunitContext();
        var items = Enumerable.Range(1, 11)
            .Select(i => new EntitySelectorItem<int>(Key: i, Text: $"Имя {i}", ExtraSearch: i == 5 ? "особый" : ""))
            .ToArray();

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
        });

        cut.Find("input[type=search]").Input("особый");

        cut.FindAll("option").Count.ShouldBe(1);
    }

    [Fact]
    public void SelectionChanged_FiresWithCorrectKey()
    {
        using var ctx = new BunitContext();
        var items = CreateItems(3);
        int[]? receivedKeys = null;

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
            parameters.Add(x => x.SelectedKeysChanged, (int[] keys) => { receivedKeys = keys; });
        });

        cut.Find("select").Change("2");

        receivedKeys.ShouldNotBeNull();
        receivedKeys.ShouldBe([2]);
    }

    [Fact]
    public void DisabledItem_RendersDisabledOption()
    {
        using var ctx = new BunitContext();
        var items = new[]
        {
            new EntitySelectorItem<int>(Key: 1, Text: "Обычный"),
            new EntitySelectorItem<int>(Key: 2, Text: "Отключён", Disabled: true),
        };

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
        });

        var options = cut.FindAll("option");
        options[0].GetAttribute("disabled").ShouldBeNull();
        options[1].GetAttribute("disabled").ShouldNotBeNull();
    }

    [Fact]
    public void Subtext_RenderedInOption()
    {
        using var ctx = new BunitContext();
        var items = new[]
        {
            new EntitySelectorItem<int>(Key: 1, Text: "Основной текст", Subtext: "Подтекст"),
        };

        var cut = ctx.Render<EntitySelector<int>>(parameters =>
        {
            parameters.Add(x => x.PossibleValues, items);
            parameters.Add(x => x.SelectedKeys, []);
        });

        cut.Find("option").TextContent.ShouldContain("Основной текст");
        cut.Find("option").TextContent.ShouldContain("Подтекст");
    }
}
