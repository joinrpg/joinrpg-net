using System;
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
      return markdownString?.Contents == null ? null : new HtmlString(Convert(markdownString));
    }

    private static string Convert(MarkdownString markdownString)
    {
      var settings = CommonMarkSettings.Default.Clone();
      settings.RenderSoftLineBreaksAsLineBreaks = true;
      return Sanitizer.Sanitize(CommonMarkConverter.Convert(markdownString.Contents, settings)).Replace("<p></p>", "");
    }
  }
}
