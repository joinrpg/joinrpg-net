using System;
using System.Linq;
using System.Web;
using CommonMark;
using JoinRpg.DataModel;
using Vereyon.Web;

namespace JoinRpg.Web.Helpers
{
  public static class MarkDownHelper
  {
    private static readonly Lazy<HtmlSanitizer> SimpleHtml5SanitizerImpl = new Lazy<HtmlSanitizer>(InitHtml5Sanitizer);

    private static HtmlSanitizer InitHtml5Sanitizer()
    {
      var sanitizer = HtmlSanitizer.SimpleHtml5Sanitizer();
      sanitizer.Tag("br");
      return sanitizer;
    }

    private static HtmlSanitizer Sanitizer => SimpleHtml5SanitizerImpl.Value;

    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    public static HtmlString ToHtmlString(this MarkdownString markdownString)
    {
      return markdownString?.Contents == null ? null : new HtmlString(Convert(markdownString.Contents));
    }

    private static string Convert(string mdContents)
    {
      var settings = CommonMarkSettings.Default.Clone();
      settings.RenderSoftLineBreaksAsLineBreaks = true;
      return Sanitizer.Sanitize(CommonMarkConverter.Convert(mdContents, settings)).Replace("<p></p>", "");
    }

    public static HtmlString TakeWords(this MarkdownString markdownString, int words)
    {
      
      if (markdownString?.Contents == null)
      {
        return null;
      }
      var idx = markdownString.Contents.TakeWhile(c => (words -= (char.IsWhiteSpace(c) ? 1 : 0)) > 0).Count();
      var mdContents = markdownString.Contents.Substring(0, idx);
      return new HtmlString(Convert(mdContents));
    }
  }
}
