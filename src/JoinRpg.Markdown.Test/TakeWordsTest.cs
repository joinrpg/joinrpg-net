namespace JoinRpg.Markdown.Test;

public class TakeWordsTest
{
    [Fact]
    public void ShouldHaveElipses()
    {
        new MarkdownString(@"Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.")
            .TakeWords(20).Contents![^3..].ShouldBe("...");
    }

    [Fact]
    public void ShortShouldBeUnchanged()
    {
        var x = new MarkdownString("Lorem lorem");
        x.TakeWords(20).ShouldBe(x);
    }
}
