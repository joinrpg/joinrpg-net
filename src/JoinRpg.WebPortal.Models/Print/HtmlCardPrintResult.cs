using JetBrains.Annotations;

namespace JoinRpg.Web.Models.Print
{
    public class HtmlCardPrintResult
    {
        public HtmlCardPrintResult(string html, CardSize cardSize, string? backgroundUrl = null)
        {
            Html = html;
            CardSize = cardSize;
            BackgroundUrl = backgroundUrl;
        }

        [PublicAPI]
        public string Html { get; }

        [PublicAPI]
        public CardSize CardSize { get; }

        [PublicAPI]
        public string? BackgroundUrl { get; }
    }

    public enum CardSize
    {
        A7,
    }
}
