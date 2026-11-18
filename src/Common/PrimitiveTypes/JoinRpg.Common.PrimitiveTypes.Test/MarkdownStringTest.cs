namespace JoinRpg.Common.PrimitiveTypes.Test;

public class MarkdownStringTest
{
    /// <summary>
    /// Регрессия: на MarkdownString действовал дефолтный лимит в 999 символов из
    /// TypedStringValueAttribute, из-за чего список персонажей падал целиком на длинном
    /// описании. Markdown длинный по природе — ограничения быть не должно.
    /// </summary>
    [Fact]
    public void LongTextIsAllowed()
    {
        var longText = new string('я', 5000);

        MarkdownString markdown = longText;

        markdown.Value.ShouldBe(longText);
    }

    [Fact]
    public void EmptyTextIsAllowed()
    {
        MarkdownString markdown = "";

        markdown.Value.ShouldBe("");
    }

    [Fact]
    public void FromOptionalAllowsLongText()
    {
        var longText = new string('a', 5000);

        MarkdownString.FromOptional(longText)!.Value.ShouldBe(longText);
    }
}
