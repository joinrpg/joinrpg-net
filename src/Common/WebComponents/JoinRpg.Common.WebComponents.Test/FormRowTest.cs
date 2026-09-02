namespace JoinRpg.Common.WebComponents.Test;

public class FormRowTest
{
    [Fact]
    public void Description_RendersDescriptionText()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .Add(x => x.Description, "Some description"));
        cut.Markup.ShouldContain("Some description");
    }

    [Fact]
    public void DescriptionFragment_RendersFragmentContent()
    {
        using var ctx = new BunitContext();
        var cut = ctx.Render<FormRow>(p => p
            .Add(x => x.Label, "Label")
            .Add(x => x.DescriptionFragment, builder => builder.AddContent(0, "Fragment description")));
        cut.Markup.ShouldContain("Fragment description");
    }
}
