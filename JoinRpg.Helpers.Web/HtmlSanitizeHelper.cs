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
      sanitizer.Tag("img").AllowAttributes("src");
      sanitizer.Tag("hr");
      sanitizer.Tag("blockquote");
      sanitizer.Tag("s");
      sanitizer.Tag("pre");
      sanitizer.Tag("code");
      return sanitizer;
    }

    /// <summary>
    /// Remove all Html from string
    /// </summary>
    /// <returns>We are returning <see cref="IHtmlString"/> to signal "no need to sanitize this again"</returns>
    [NotNull]
    public static IHtmlString RemoveHtml([NotNull] this UnSafeHtml unsafeHtml)
    {
      if (unsafeHtml == null) throw new ArgumentNullException(nameof(unsafeHtml));
      return new HtmlString(RemoveAllHtmlSanitizer.Value.Sanitize(unsafeHtml.UnValidatedValue));
    }

    [NotNull]
    public static IHtmlString SanitizeHtml([NotNull] this UnSafeHtml unsafeHtml)
    {
      if (unsafeHtml == null) throw new ArgumentNullException(nameof(unsafeHtml));
      return new HtmlString(SimpleHtml5Sanitizer.Value.Sanitize(unsafeHtml.UnValidatedValue));
    }

    [NotNull]
    public static IHtmlString SanitizeHtml([NotNull] this string str)
    {
      var unsafeHtml = (UnSafeHtml) str;
      if (unsafeHtml == null) throw new ArgumentNullException(nameof(str));
      return unsafeHtml.SanitizeHtml();
    }

    [NotNull]
    public static IHtmlString WithDefaultStringValue([NotNull] this IHtmlString toHtmlString, [NotNull] string defaultValue)
    {
      if (toHtmlString == null) throw new ArgumentNullException(nameof(toHtmlString));
      if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));
      return new HtmlString(toHtmlString.ToHtmlString().WithDefaultStringValue(defaultValue));
    }
  }
}
