using System;
using Vereyon.Web;

namespace Joinrpg.Markdown
{
    internal static class HtmlSanitizers
    {
        private static readonly Lazy<HtmlSanitizer> SimpleHtml5Sanitizer = new Lazy<HtmlSanitizer>(InitHtml5Sanitizer);
        public static IHtmlSanitizer Simple { get; } = SimpleHtml5Sanitizer.Value;

        private static readonly Lazy<HtmlSanitizer> RemoveAllHtmlSanitizer = new Lazy<HtmlSanitizer>(InitRemoveSanitizer);
        public static IHtmlSanitizer RemoveAll { get; } = RemoveAllHtmlSanitizer.Value;

        private static HtmlSanitizer InitRemoveSanitizer()
        {
            return new HtmlSanitizer()
                .FlattenTags("p", "h1", "h2", "h3", "h4", "h5",
                    "strong", "b", "i", "em", "br", "p", "div", "span", "ul", "ol",
                    "li", "a", "blockquote");
        }

        private static HtmlSanitizer FlattenTags(this HtmlSanitizer htmlSanitizer, params string[] tagNames)
        {
            foreach (var tagName in tagNames)
            {
                FlattenTag(tagName);
            }
            return htmlSanitizer;

            void FlattenTag(string s)
            {
                htmlSanitizer.Tag(s).Operation(SanitizerOperation.FlattenTag);
            }
        }

        private static HtmlSanitizer InitHtml5Sanitizer()
        {
            var sanitizer = HtmlSanitizer.SimpleHtml5Sanitizer();
            sanitizer.Tag("img").AllowAttributes("src");
            sanitizer.Tag("hr");
            sanitizer.Tag("blockquote");
            sanitizer.Tag("s");
            sanitizer.Tag("pre");
            sanitizer.Tag("code");
            return sanitizer;
        }
    }
}
