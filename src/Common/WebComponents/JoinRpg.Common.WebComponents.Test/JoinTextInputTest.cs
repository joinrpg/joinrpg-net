namespace JoinRpg.Common.WebComponents.Test;

public class JoinTextInputTest
{
    [Fact]
    public void Change_TrimsValueAndField()
    {
        using var ctx = new BunitContext();
        string? captured = null;
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("   Тест   ");

        captured.ShouldBe("Тест");
        cut.Find("input").GetAttribute("value").ShouldBe("Тест");
    }

    /// <summary>
    /// При вводе по нажатию пробел в конце — ещё не конец ввода: наружу уходит обрезанное значение,
    /// но в поле пробел остаётся, иначе не набрать текст из двух слов.
    /// </summary>
    [Fact]
    public void Immediate_TrailingSpaceStaysInFieldUntilBlur()
    {
        using var ctx = new BunitContext();
        string? captured = null;
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Immediate, true)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Input("Моя ");
        cut.Render(p => p.Add(x => x.Value, captured!));

        captured.ShouldBe("Моя");
        cut.Find("input").GetAttribute("value").ShouldBe("Моя ");

        cut.Find("input").Blur();

        cut.Find("input").GetAttribute("value").ShouldBe("Моя");
    }

    [Fact]
    public void Immediate_ExternalChangeOverwritesField()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Immediate, true));

        cut.Find("input").Input("Моя ");
        cut.Render(p => p.Add(x => x.Value, "Другое"));

        cut.Find("input").GetAttribute("value").ShouldBe("Другое");
    }

    [Fact]
    public void TrimDisabled_KeepsSpaces()
    {
        using var ctx = new BunitContext();
        string? captured = null;
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Trim, false)
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("  Тест  ");

        captured.ShouldBe("  Тест  ");
    }

    [Fact]
    public void Variants_RendersDatalistAndListAttribute()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Variants, ["300", "500", "800"]));

        var listId = cut.Find("input").GetAttribute("list");
        listId.ShouldNotBeNullOrEmpty();

        var options = cut.FindAll($"datalist#{listId} option");
        options.Select(o => o.GetAttribute("value")).ShouldBe(["300", "500", "800"]);
    }

    [Fact]
    public void NoVariants_NoDatalistAndNoListAttribute()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p.Add(x => x.Value, ""));

        cut.Find("input").HasAttribute("list").ShouldBeFalse();
        cut.FindAll("datalist").ShouldBeEmpty();
    }

    [Fact]
    public void Name_RendersNameAttribute()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Name, "CharacterTypeInfo.SlotName"));

        cut.Find("input").GetAttribute("name").ShouldBe("CharacterTypeInfo.SlotName");
    }

    [Fact]
    public void WithoutNameAndExpression_NameAttributeNotRendered()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p.Add(x => x.Value, ""));

        cut.Find("input").HasAttribute("name").ShouldBeFalse();
    }

    [Fact]
    public void Required_RendersDataValAttributesAndValidationSpan()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Name, "SlotName")
            .Add(x => x.Required, true));

        var input = cut.Find("input");
        input.GetAttribute("data-val").ShouldBe("true");
        input.GetAttribute("data-val-required").ShouldBe("Это поле обязательно к заполнению");

        var span = cut.Find("span.field-validation-valid");
        span.GetAttribute("data-valmsg-for").ShouldBe("SlotName");
    }

    [Fact]
    public void Required_CustomMessageIsUsed()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Required, true)
            .Add(x => x.RequiredMessage, "Укажите название"));

        cut.Find("input").GetAttribute("data-val-required").ShouldBe("Укажите название");
    }

    [Fact]
    public void NotRequired_NoDataValAttributesAndNoSpan()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Name, "SlotName"));

        var input = cut.Find("input");
        input.HasAttribute("data-val").ShouldBeFalse();
        input.HasAttribute("data-val-required").ShouldBeFalse();
        cut.FindAll("span.field-validation-valid").ShouldBeEmpty();
    }

    [Fact]
    public void ExplicitId_IsUsedForDataValmsgFor()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinTextInput>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Required, true)
            .AddUnmatched("id", "my-field"));

        cut.Find("input").Id.ShouldBe("my-field");
        cut.Find("span.field-validation-valid").GetAttribute("data-valmsg-for").ShouldBe("my-field");
    }

    /// <summary>
    /// Внутри FormRow id поля генерирует строка, но data-valmsg-for обязан ссылаться на name:
    /// jquery.validate.unobtrusive ищет span ошибки по input.name (как Html.ValidationMessageFor).
    /// </summary>
    [Fact]
    public void RequiredInFormRow_ValidationSpanPointsToName()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Название")
            .AddChildContent<JoinTextInput>(f => f
                .Add(x => x.Value, "")
                .Add(x => x.Name, "CharacterTypeInfo.SlotName")
                .Add(x => x.Required, true)));

        cut.Find("input").Id.ShouldNotBe("CharacterTypeInfo.SlotName");
        cut.Find("span.field-validation-valid").GetAttribute("data-valmsg-for")
            .ShouldBe("CharacterTypeInfo.SlotName");
    }
}
