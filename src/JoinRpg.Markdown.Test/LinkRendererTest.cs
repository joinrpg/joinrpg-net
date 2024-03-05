using JoinRpg.DataModel;
using Shouldly;
using Xunit;

namespace JoinRpg.Markdown.Test;

public class LinkRendererTest
{
    private class LinkRendererMock : ILinkRenderer
    {
        private const string Test = "персонаж";

        public IEnumerable<string> LinkTypesToMatch { get; } = new[] { Test };

        public string Render(string match, int index, string extra)
        {
            match.ShouldBe("%" + Test);
            index.ShouldBeGreaterThan(0);
            return $"<b>{index}</b>{extra}";
        }
    }


    private void NoMatch(string contents)
        => new MarkdownString(contents).ToPlainText(_mock).ToHtmlString().ShouldBe(contents);

    private void Match(string expected, string original)
        => new MarkdownString(original).ShouldBeHtml(expected, _mock);

    private readonly LinkRendererMock _mock = new();

    [Fact]
    public void TestSimpleMatch() => Match("<p><strong>12</strong></p>", "%персонаж12");

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
