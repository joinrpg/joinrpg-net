namespace JoinRpg.Common.WebComponents.Test;

public class NumberInputTest
{
    [Theory]
    [InlineData("42", 42)]
    [InlineData("-5", -5)]
    [InlineData("", null)]
    [InlineData("1.5", null)]
    [InlineData("1e3", null)]
    [InlineData("99999999999", null)]
    public void Change_InvokesValueChangedWithParsedValueOrNull(string input, int? expected)
    {
        using var ctx = new BunitContext();
        int? captured = 7;
        var cut = ctx.Render<NumberInput>(p => p
            .Add(x => x.Name, "Number")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("input").Change(input);

        captured.ShouldBe(expected);
    }
}
