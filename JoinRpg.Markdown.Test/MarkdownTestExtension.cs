using Joinrpg.Markdown;
using JoinRpg.DataModel;
using Shouldly;

namespace JoinRpg.Markdown.Test
{
    public static class MarkdownTestExtension
    {
        /// <summary>
        /// Ensures that some markdown string will be converted to some HTML
        /// </summary>
        public static void ShouldBeHtml(this string markdown, string expectedHtml) =>
            new MarkdownString(markdown).ShouldBeHtml(expectedHtml);

        /// <summary>
        /// Ensures that some markdown string will be converted to some HTML
        /// </summary>
        public static void ShouldBeHtml(this MarkdownString markdownString, string expectedHtml, ILinkRenderer linkRenderer = null) =>
            markdownString.ToHtmlString(linkRenderer).ToHtmlString().ShouldBe(expectedHtml);
    }
}
