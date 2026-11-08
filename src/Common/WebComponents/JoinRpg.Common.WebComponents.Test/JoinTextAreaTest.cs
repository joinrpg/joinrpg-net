namespace JoinRpg.Common.WebComponents.Test;

public class JoinTextAreaTest
{
    private static BunitContext NewContext()
    {
        var ctx = new BunitContext();
        ctx.JSInterop.Mode = JSRuntimeMode.Loose;
        return ctx;
    }

    [Fact]
    public void Counter_NotShown_WhenOnlyMaxLength()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000));

        cut.FindAll(".text-muted").ShouldBeEmpty();
    }

    [Fact]
    public void Counter_NotShown_WhenOnlyShowCounter()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.ShowCounter, true));

        cut.FindAll(".text-muted").ShouldBeEmpty();
    }

    [Fact]
    public void Counter_Shown_WhenShowCounterAndMaxLength()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true));

        cut.Find(".text-muted").TextContent.ShouldBe("0/3000");
    }

    [Fact]
    public void Counter_StartsFromValueLength()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "текст")
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true));

        cut.Find(".text-muted").TextContent.ShouldBe("5/3000");
    }

    [Fact]
    public void Counter_StartsFromZero_WhenValueNull()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, null)
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true));

        cut.Find(".text-muted").TextContent.ShouldBe("0/3000");
    }

    [Fact]
    public void Counter_UpdatesOnInput()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true));

        cut.Find("textarea").Input("привет");

        cut.Find(".text-muted").TextContent.ShouldBe("6/3000");
    }

    [Fact]
    public void MaxLength_RendersMaxlengthAttribute()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000));

        cut.Find("textarea").GetAttribute("maxlength").ShouldBe("3000");
    }

    [Fact]
    public void MaxLength_NotSet_MaxlengthAttributeAbsent()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, ""));

        cut.Find("textarea").GetAttribute("maxlength").ShouldBeNull();
    }

    [Fact]
    public void MaxLength_LowercaseAttribute_BindsToParameterInSync()
    {
        using var ctx = NewContext();
        var cut = ctx.Render<JoinTextArea>(p => p
            .Add(x => x.Value, "")
            .Add(x => x.MaxLength, 3000)
            .Add(x => x.ShowCounter, true)
            .AddUnmatched("maxlength", 5000));

        cut.Find("textarea").GetAttribute("maxlength").ShouldBe("5000");
        cut.Find(".text-muted").TextContent.ShouldBe("0/5000");
    }
}
