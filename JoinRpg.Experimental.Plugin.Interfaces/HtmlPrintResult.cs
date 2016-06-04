using JetBrains.Annotations;

namespace JoinRpg.Experimental.Plugin.Interfaces
{
  public class HtmlCardPrintResult
  {
    public HtmlCardPrintResult(string html, CardSize cardSize)
    {
      Html = html;
      CardSize = cardSize;
    }

    [PublicAPI]
    public string Html { get; }

    [PublicAPI]
    public CardSize CardSize { get; }
  }

  public enum CardSize
  {
    A6
  }
}
