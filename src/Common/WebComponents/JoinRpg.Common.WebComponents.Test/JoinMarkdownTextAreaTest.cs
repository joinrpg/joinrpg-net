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

    [Fact]
    public void Placeholder_IsPassedToTextArea()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<JoinMarkdownTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.Placeholder, "Комментарий"));

        cut.Find("textarea").GetAttribute("placeholder").ShouldBe("Комментарий");
    }

    [Fact]
    public void Counter_Shown_WhenShowCounterAndMaxLength()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<JoinMarkdownTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true));

        cut.Find(".text-muted").TextContent.ShouldBe("0/3000");
    }

    [Fact]
    public void MaxLength_RendersMaxlengthAttribute()
    {
        using var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        var cut = ctx.Render<JoinMarkdownTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000));

        cut.Find("textarea").GetAttribute("maxlength").ShouldBe("3000");
    }
}
