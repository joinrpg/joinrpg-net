using JetBrains.Annotations;
using JoinRpg.Helpers.Web;

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
    public UnSafeHtml Html { get; }

    [PublicAPI]
    public CardSize CardSize { get; }
  }

  public enum CardSize
  {
    A7
  }
}
