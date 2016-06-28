using System.Linq;
using System.Web;
using CommonMark;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;

namespace JoinRpg.Web.Helpers
{
  public static class MarkDownHelper
  {
    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    [CanBeNull]
    public static HtmlString ToHtmlString([CanBeNull] this MarkdownString markdownString)
    {
      return markdownString?.Contents == null ? null : markdownString.RenderMarkDownToHtmlUnsafe().SanitizeHtml();
    }

    public static string ToPlainText([CanBeNull] this MarkdownString markdownString)
    {
      if (markdownString?.Contents == null)
      {
        return null;
      }
      return markdownString.RenderMarkDownToHtmlUnsafe().RemoveHtml().Trim();
    }

    private static UnSafeHtml RenderMarkDownToHtmlUnsafe(this MarkdownString markdownString)
    {
      var settings = CommonMarkSettings.Default.Clone();
      settings.RenderSoftLineBreaksAsLineBreaks = true;
      return CommonMarkConverter.Convert(markdownString.Contents, settings);
    }

    public static MarkdownString TakeWords(this MarkdownString markdownString, int words)
    {
      if (markdownString?.Contents == null)
      {
        return null;
      }
      var w = words;
      var idx = markdownString.Contents.TakeWhile(c => (w -= char.IsWhiteSpace(c) ? 1 : 0) > 0 && c != '\n').Count();
      var mdContents = markdownString.Contents.Substring(0, idx);
      return new MarkdownString(mdContents);
    }
  }
}
