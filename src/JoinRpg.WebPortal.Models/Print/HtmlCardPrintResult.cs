namespace JoinRpg.Web.Models.Print;

public class HtmlCardPrintResult
{
    public HtmlCardPrintResult(string html, CardSize cardSize)
    {
        Html = html;
        CardSize = cardSize;
    }

    public string Html { get; }

    public CardSize CardSize { get; }
}

public enum CardSize
{
    A7,
}
