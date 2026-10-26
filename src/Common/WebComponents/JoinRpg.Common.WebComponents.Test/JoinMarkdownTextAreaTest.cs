namespace JoinRpg.Common.WebComponents.Test;

public class JoinMarkdownTextAreaTest
{
    [Fact]
    public void Change_IsPassedToValueChanged()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var captured = "";
        var cut = ctx.Render<JoinMarkdownTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.ValueChanged, v => captured = v));

        cut.Find("textarea").Change("**текст**");

        captured.ShouldBe("**текст**");
    }
}
