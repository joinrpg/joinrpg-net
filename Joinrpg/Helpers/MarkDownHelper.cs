using System;
using System.Web;
using CommonMark;
using JoinRpg.DataModel;
using Vereyon.Web;

namespace JoinRpg.Web.Helpers
{
  public static class MarkDownHelper
  {
    private static readonly Lazy<HtmlSanitizer> SimpleHtml5SanitizerImpl = new Lazy<HtmlSanitizer>(HtmlSanitizer.SimpleHtml5Sanitizer);
    private static HtmlSanitizer Sanitizer => SimpleHtml5SanitizerImpl.Value;

    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    public static HtmlString ToHtmlString(this MarkdownString markdownString)
    {
      if (markdownString?.Contents == null)
      {
        return null;
      }
      var markdownResults = CommonMarkConverter.Convert(markdownString.Contents);
      var sanitazeResults = Sanitizer.Sanitize(markdownResults);
      return new HtmlString(sanitazeResults);
    }
  }
}
