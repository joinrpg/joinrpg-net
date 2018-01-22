using Joinrpg.Markdown;
using JoinRpg.DataModel;
using Shouldly; using Xunit;

namespace JoinRpg.Markdown.Test
{

    public class ListTest
  {
    [Fact]
    public void TestListFromSix()
      => TestMarkdown(
@"
6. something
7. something",
"<ol start=\"6\">\n<li>something</li>\n<li>something</li>\n</ol>");

    [Fact]
    public void TestBr()
      => TestMarkdown(
@"test
break",
"<p>test<br>\nbreak</p>");

      private void TestMarkdown(string markdown, string expectedHtml) =>
          new MarkdownString(markdown).ToHtmlString().ToHtmlString().ShouldBe(expectedHtml);
  }
}
