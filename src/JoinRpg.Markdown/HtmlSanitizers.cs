using System;
using HtmlAgilityPack;
using Vereyon.Web;

namespace JoinRpg.Markdown
{
    internal static class HtmlSanitizers
    {
        private static readonly Lazy<HtmlSanitizer> SimpleHtml5Sanitizer = new(InitHtml5Sanitizer);
        public static IHtmlSanitizer Simple { get; } = SimpleHtml5Sanitizer.Value;

        private static readonly Lazy<HtmlSanitizer> RemoveAllHtmlSanitizer = new(InitRemoveSanitizer);
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
                _ = htmlSanitizer.Tag(s).Operation(SanitizerOperation.FlattenTag);
            }
        }

        private static HtmlSanitizer InitHtml5Sanitizer()
        {
            var sanitizer = HtmlSanitizer.SimpleHtml5Sanitizer();
            _ = sanitizer.Tag("img")
               .CheckAttributeUrl("src")
               .AllowAttributes("height")
               .AllowAttributes("width")
               .AllowAttributes("alt");
            _ = sanitizer
                .Tag("iframe")
                .SanitizeAttributes("src", AllowWhiteListedIframeDomains.Default)
                .AllowAttributes("height")
                .AllowAttributes("width")
                .AllowAttributes("frameborder");
            _ = sanitizer.Tag("hr");
            _ = sanitizer.Tag("blockquote");
            _ = sanitizer.Tag("s");
            _ = sanitizer.Tag("pre");
            _ = sanitizer.Tag("code");
            return sanitizer;
        }
    }

    internal class AllowWhiteListedIframeDomains : UrlCheckerAttributeSanitizer
    {
        private AllowWhiteListedIframeDomains() { }
        public static new AllowWhiteListedIframeDomains Default { get; private set; } = new AllowWhiteListedIframeDomains();

        protected override bool AttributeUrlCheck(HtmlAttribute attribute)
        {
            var baseResult = base.AttributeUrlCheck(attribute);
            if (!baseResult)
            {
                return false;
            }

            if (attribute.Value.StartsWith("https://music.yandex.ru/iframe/")
                || attribute.Value.StartsWith("https://www.youtube.com/embed/")
                || attribute.Value.StartsWith("https://ok.ru/videoembed/")
                )
            {
                return true;
            }

            return false;
        }
    }
}
