using System;
using System.Web;
using JetBrains.Annotations;
using Vereyon.Web;

namespace JoinRpg.Helpers.Web
{
  public static class HtmlSanitizeHelper
  {
    private static readonly Lazy<HtmlSanitizer> SimpleHtml5Sanitizer = new Lazy<HtmlSanitizer>(InitHtml5Sanitizer);
    private static readonly Lazy<HtmlSanitizer> RemoveAllHtmlSanitizer = new Lazy<HtmlSanitizer>(InitRemoveSanitizer);

    private static HtmlSanitizer InitRemoveSanitizer()
    {
      return new HtmlSanitizer()
        .FlattenTags("p", "h1", "h2", "h3", "h4", "h5", "strong", "b", "i", "em", "br", "p", "div", "span", "ul", "ol",
          "li", "a", "blockquote");
    }

    private static void FlattenTag(this HtmlSanitizer htmlSanitizer, string tagName)
    {
      htmlSanitizer.Tag(tagName).Operation(SanitizerOperation.FlattenTag);
    }

    private static HtmlSanitizer FlattenTags(this HtmlSanitizer htmlSanitizer, params string[] tagNames)
    {
      foreach (var tagName in tagNames)
      {
        htmlSanitizer.FlattenTag(tagName);
      }
      return htmlSanitizer;
    }

    private static HtmlSanitizer InitHtml5Sanitizer()
    {
      var sanitizer = HtmlSanitizer.SimpleHtml5Sanitizer();
      sanitizer.Tag("br");
      sanitizer.Tag("img").AllowAttributes("src");
      sanitizer.Tag("hr");
      sanitizer.Tag("p");
      sanitizer.Tag("blockquote");
      sanitizer.Tag("s");
      sanitizer.Tag("pre");
      sanitizer.Tag("code");
      return sanitizer;
    }

    public static string RemoveHtml(this UnSafeHtml unsafeHtml)
    {
      return RemoveAllHtmlSanitizer.Value.Sanitize(unsafeHtml.UnValidatedValue);
    }

    [NotNull]
    public static HtmlString SanitizeHtml([NotNull] this UnSafeHtml unsafeHtml)
    {
      if (unsafeHtml == null) throw new ArgumentNullException(nameof(unsafeHtml));
      return new HtmlString(SimpleHtml5Sanitizer.Value.Sanitize(unsafeHtml.UnValidatedValue));
    }
  }
}
