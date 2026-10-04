namespace JoinRpg.Common.WebComponents.Test;

public class JoinRpgTimePickerTest
{
    [Fact]
    public void RendersTimeInputWithNameValue()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.Value, new TimeOnly(9, 5)));

        var input = cut.Find("input");
        input.GetAttribute("type").ShouldBe("time");
        input.GetAttribute("name").ShouldBe("StartTime");
        input.GetAttribute("value").ShouldBe("09:05");
    }

    [Theory]
    [InlineData("10:30")]
    [InlineData("10:30:00")]
    public void Change_ValidTime_InvokesValueChangedWithParsedTime(string value)
    {
        using var ctx = new BunitContext();
        TimeOnly? captured = null;
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change(value);

        captured.ShouldBe(new TimeOnly(10, 30));
    }

    [Fact]
    public void Change_EmptyValue_InvokesValueChangedWithNull()
    {
        using var ctx = new BunitContext();
        TimeOnly? captured = new TimeOnly(10, 30);
        var cut = ctx.Render<JoinRpgTimePicker>(p => p
            .Add(x => x.Name, "StartTime")
            .Add(x => x.Value, new TimeOnly(10, 30))
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change("");

        captured.ShouldBeNull();
    }
}
