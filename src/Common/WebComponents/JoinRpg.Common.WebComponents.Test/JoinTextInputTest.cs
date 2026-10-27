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
}
