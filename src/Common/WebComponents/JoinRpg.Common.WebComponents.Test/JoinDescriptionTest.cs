namespace JoinRpg.Common.WebComponents.Test;

public class JoinDescriptionTest
{
    /// <summary>
    /// Пояснение рендерится со своим классом (оформление — scoped CSS компонента),
    /// а не классом <c>help-block</c> из Bootstrap 3.
    /// </summary>
    [Fact]
    public void RendersChildContentWithOwnCssClass()
    {
        using var ctx = new BunitContext();

        var cut = ctx.Render<JoinDescription>(p => p
            .AddChildContent("Пояснение"));

        var description = cut.Find(".join-description");
        description.TextContent.ShouldBe("Пояснение");
        cut.FindAll(".help-block").ShouldBeEmpty();
    }
}
