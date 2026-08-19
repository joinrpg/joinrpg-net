namespace JoinRpg.Common.WebComponents.Test;

public class JoinUserLinkEditorTest
{
    [Fact]
    public void SingleMode_RendersExactlyOneRow_AndNoRemoveButton()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, false)
            .Add(x => x.Values, ["123"]));

        cut.FindAll("input").Count.ShouldBe(1);
        cut.FindAll("button").Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleMode_EmptyValues_RendersOneEmptyRow()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, []));

        cut.FindAll("input").Count.ShouldBe(1);
        cut.FindAll("button").Count.ShouldBe(0);
    }

    [Fact]
    public void MultipleMode_FillingLastRow_AddsNewEmptyRow()
    {
        using var ctx = new BunitContext();
        List<string> captured = ["a"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input").Count.ShouldBe(2);

        cut.FindAll("input")[1].Input("b");

        captured.ShouldBe(["a", "b"]);
        cut.FindAll("input").Count.ShouldBe(3);
        cut.FindAll("button").Count.ShouldBe(2);
    }

    [Fact]
    public void MultipleMode_ClearingLastRealRow_CollapsesToVirtualEmptyRow()
    {
        using var ctx = new BunitContext();
        List<string> captured = ["a", "b"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input")[1].Input("");

        captured.ShouldBe(["a"]);
        cut.FindAll("input").Count.ShouldBe(2);
    }

    [Fact]
    public void MultipleMode_RemovingRow_DoesNotLeaveStaleValueInReusedRow()
    {
        using var ctx = new BunitContext();
        List<string> captured = ["a", "b"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("button")[1].Click();

        captured.ShouldBe(["a"]);
        cut.FindAll("input").Count.ShouldBe(2);
        cut.Markup.ShouldNotContain("value=\"b\"");
    }

    [Fact]
    public void MultipleMode_TypingWithoutBlur_AddsNewEmptyRowImmediately()
    {
        using var ctx = new BunitContext();
        List<string> captured = ["a"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("input")[1].Input("b");

        cut.FindAll("input").Count.ShouldBe(3);
    }

    [Fact]
    public void MultipleMode_RemoveButton_RemovesRowFromMiddle()
    {
        using var ctx = new BunitContext();
        List<string> captured = ["a", "b", "c"];
        var cut = ctx.Render<JoinUserLinkEditor>(p => p
            .Add(x => x.Multiple, true)
            .Add(x => x.Values, captured)
            .Add(x => x.ValuesChanged, v => captured = v));

        cut.FindAll("button")[0].Click();

        captured.ShouldBe(["b", "c"]);
        cut.FindAll("input").Count.ShouldBe(3);
        cut.FindAll("button").Count.ShouldBe(2);
    }
}
