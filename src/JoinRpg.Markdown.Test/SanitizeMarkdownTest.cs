using Xunit;

namespace JoinRpg.Markdown.Test
{
    public class SanitizeMarkdownTest
    {
        [Fact]
        public void BShouldBePreserved() => "<b>1</b>".ShouldBeHtml("<p><strong>1</strong></p>");

        [Fact]
        public void MixedMarkdown() => "<b>_1_</b>".ShouldBeHtml("<p><strong><em>1</em></strong></p>");
    }
}
