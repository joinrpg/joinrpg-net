namespace JoinRpg.Common.WebComponents.Test;

public class JoinRpgDatePickerTest
{
    [Fact]
    public void RendersDateInputWithNameValueMinMax()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.Value, new DateOnly(2026, 1, 15))
            .Add(x => x.Min, new DateOnly(2026, 1, 1))
            .Add(x => x.Max, new DateOnly(2026, 12, 31)));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("date");
        input.GetAttribute("name").ShouldBe("OperationDate");
        input.GetAttribute("value").ShouldBe("2026-01-15");
        input.GetAttribute("min").ShouldBe("2026-01-01");
        input.GetAttribute("max").ShouldBe("2026-12-31");
    }

    [Fact]
    public void Change_ValidDate_InvokesValueChangedWithParsedDate()
    {
        using var ctx = new BunitContext();
        DateOnly? captured = null;
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("2026-01-15");

        captured.ShouldBe(new DateOnly(2026, 1, 15));
    }

    [Fact]
    public void Change_EmptyValue_InvokesValueChangedWithNull()
    {
        using var ctx = new BunitContext();
        DateOnly? captured = new DateOnly(2026, 1, 15);
        var cut = ctx.Render<JoinRpgDatePicker>(p => p
            .Add(x => x.Name, "OperationDate")
            .Add(x => x.Value, new DateOnly(2026, 1, 15))
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("");

        captured.ShouldBeNull();
    }
}
