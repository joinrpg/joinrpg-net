namespace JoinRpg.Web.Models.Print;

public class HtmlCardPrintResult
{
    public HtmlCardPrintResult(string html, CardSize cardSize, string? backgroundUrl = null)
    {
        Html = html;
        CardSize = cardSize;
        BackgroundUrl = backgroundUrl;
    }

    public string Html { get; }

    public CardSize CardSize { get; }

    public string? BackgroundUrl { get; }
}

public enum CardSize
{
    A7,
}
