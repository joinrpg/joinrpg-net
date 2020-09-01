using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
    public class HtmlCardPrintResult
    {
        public HtmlCardPrintResult(string html, CardSize cardSize, string backgroundUrl = null)
        {
            Html = html;
            CardSize = cardSize;
            BackgroundUrl = backgroundUrl;
        }

        [PublicAPI]
        public string Html { get; }

        [PublicAPI]
        public CardSize CardSize { get; }

        [PublicAPI, CanBeNull]
        public string BackgroundUrl { get; }
    }

    public enum CardSize
    {
        A7,
    }
}
