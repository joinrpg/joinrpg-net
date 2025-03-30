namespace JoinRpg.Markdown.Test;

public class LinkRendererTest
{
    private class LinkRendererMock : ILinkRenderer
    {
        private const string Test = "персонаж";

        public string[] LinkTypesToMatch { get; } = [Test];

        public string Render(string match, int index, string extra)
        {
            match.ShouldBe("%" + Test);
            index.ShouldBeGreaterThan(0);
            return $"<b>{index}</b>{extra}";
        }
    }

    private class LinkRendererMock2 : ILinkRenderer
    {
        private const string Test = "контакты";

        public string[] LinkTypesToMatch { get; } = [Test];

        public string Render(string match, int index, string extra)
        {
            match.ShouldBe("%" + Test);
            index.ShouldBeGreaterThan(0);
            return $"<b>{index}</b>{extra}";
        }
    }


    private void NoMatch(string contents)
        => new MarkdownString(contents).ShouldBeHtml("<p>" + contents + "</p>");

    private void Match(string expected, string original)
        => new MarkdownString(original).ShouldBeHtml(expected, _mock);

    private readonly LinkRendererMock _mock = new();

    [Fact]
    public void TestIgnoredIfDisabled() => new MarkdownString("%персонаж12").ToPlainText().ShouldBe("%персонаж12");

    [Fact]
    public void TestSimpleMatch() => Match("<p><strong>12</strong></p>", "%персонаж12");

    //Test that pipeline uses correct renderer, not prev. one
    [Fact]
    public void TestAnotherMatch() => new MarkdownString("%контакты12").ShouldBeHtml("<p><strong>12</strong></p>", new LinkRendererMock2());

    [Fact]
    public void TestNoMatchWithoutIndex() => NoMatch("%персонаж");

    [Fact]
    public void TestNoMatchInMiddle() => NoMatch("test%персонаж12");

    [Fact]
    public void TestMatchWithExtra() => Match("<p><strong>121</strong>extra</p>", "%персонаж121(extra)");

    [Fact]
    public void TestNoMatchZero() => NoMatch("%персонаж0(extra)");

    [Fact]
    public void TestMiddleOfSentence() => Match("<p>s <strong>12</strong></p>", "s %персонаж12");
}
