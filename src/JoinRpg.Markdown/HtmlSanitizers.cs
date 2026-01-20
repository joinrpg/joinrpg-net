using HtmlAgilityPack;
using Vereyon.Web;

namespace JoinRpg.Markdown;

internal static partial class HtmlSanitizers
{
    private static HtmlSanitizerPool SimpleSanitizers { get; } = new (InitHtml5Sanitizer);

    /// <returns>An instance of <see cref="IDisposableHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IDisposableHtmlSanitizer GetSimple() => SimpleSanitizers.Get();

    private static HtmlSanitizerPool RemoveAllSanitizers { get; } = new(InitRemoveSanitizer);

    /// <returns>An instance of <see cref="IDisposableHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IDisposableHtmlSanitizer GetRemoveAll() => RemoveAllSanitizers.Get();

    private static HtmlSanitizerPool TelegramSanitizers { get; } = new(InitTelegramSanitizer);

    /// <returns>An instance of <see cref="IDisposableHtmlSanitizer"/> which has to be disposed right after use.</returns>
    public static IDisposableHtmlSanitizer GetTelegram() => TelegramSanitizers.Get();

    private static HtmlSanitizer InitRemoveSanitizer()
    {
        var x = new HtmlSanitizer();
        _ = x.Tags("p", "h1", "h2", "h3", "h4", "h5",
                "strong", "b", "i", "em", "br", "p", "div", "span", "ul", "ol",
                "li", "a", "blockquote")
            .Flatten();

        return x;
    }

    private static HtmlSanitizer InitHtml5Sanitizer()
    {
        var sanitizer = HtmlSanitizer.SimpleHtml5Sanitizer();
        _ = sanitizer.AllowCss("table");
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

        sanitizer.Tags("hr", "blockquote", "s", "pre", "code", "table", "thead", "tbody", "tr", "td", "th");

        return sanitizer;
    }

    private static HtmlSanitizer InitTelegramSanitizer()
    {
        var htmlSanitizer = new HtmlSanitizer
        {
            WhiteListMode = true
        };
        htmlSanitizer.Tags("b", "strong", "i", "em", "u", "ins", "s", "strike", "del").RemoveEmpty();

        htmlSanitizer.Tag("a").SetAttribute("target", "_blank").SetAttribute("rel", "nofollow")
            .CheckAttributeUrl("href")
            .RemoveEmpty()
            .NoAttributes(SanitizerOperation.FlattenTag);


        htmlSanitizer.Tags("code", "pre", "blockquote").RemoveEmpty();

        htmlSanitizer.Tags("p", "div", "span", "ul", "ol", "li").Flatten();

        return htmlSanitizer;
    }


    private class AllowWhiteListedIframeDomains : UrlCheckerAttributeSanitizer
    {
        private AllowWhiteListedIframeDomains() { }
        public static AllowWhiteListedIframeDomains Default { get; private set; } = new AllowWhiteListedIframeDomains();

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


