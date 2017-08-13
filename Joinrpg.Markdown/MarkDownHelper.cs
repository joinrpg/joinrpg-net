using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using Markdig;

namespace Joinrpg.Markdown
{
  public interface ILinkRenderer
  {
    IEnumerable<string> LinkTypesToMatch { get; }
    string Render(string match, int index, string extra);
  }

  public static class MarkDownHelper
  {
    private static readonly DoNothingLinkRenderer NoLinks = new DoNothingLinkRenderer();

    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    [NotNull]
    public static IHtmlString ToHtmlString([CanBeNull] this MarkdownString markdownString,
      [NotNull] ILinkRenderer renderer)
    {
      if (renderer == null) throw new ArgumentNullException(nameof(renderer));
      return markdownString?.Contents == null ? new HtmlString("") : markdownString.RenderMarkDownToHtmlUnsafe(renderer).SanitizeHtml();
    }

    [NotNull]
    public static IHtmlString ToHtmlString([CanBeNull] this MarkdownString markdownString)
    {
      return markdownString.ToHtmlString(NoLinks);
    }

    [NotNull]
    public static IHtmlString ToPlainText([CanBeNull] this MarkdownString markdownString)
    {
      return markdownString.ToPlainText(NoLinks);
    }

    private class DoNothingLinkRenderer : ILinkRenderer
    {
      public IEnumerable<string> LinkTypesToMatch { get; } = new string[] {};
      public string Render(string match, int index, string extra)
      {
        throw new NotImplementedException();
      }
    }

    [NotNull]
    public static IHtmlString ToPlainText([CanBeNull] this MarkdownString markdownString, [NotNull] ILinkRenderer renderer)
    {
      if (renderer == null) throw new ArgumentNullException(nameof(renderer));
      if (markdownString?.Contents == null)
      {
        return new HtmlString("");
      }
      return markdownString.RenderMarkDownToHtmlUnsafe(renderer).RemoveHtml();
    }

    private static UnSafeHtml RenderMarkDownToHtmlUnsafe(this MarkdownString markdownString,
      [NotNull] ILinkRenderer renderer)
    {
      if (renderer == null) throw new ArgumentNullException(nameof(renderer));
      //TODO - do we need to save re-use pipeline?
      var pipeline =
        new MarkdownPipelineBuilder()
          .UseSoftlineBreakAsHardlineBreak()
          .UseAutoLinks()
          .UseEntityLinker(renderer)
          .Build();
      return Markdig.Markdown.ToHtml(markdownString.Contents, pipeline);
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

    public static MarkdownString HighlightDiffPlaceholder(string newString, string oldString)
    {
      //TODO: look for diff algorithms
      return new MarkdownString(newString);
    }
    public static MarkdownString HighlightDiffPlaceholder(MarkdownString newString, MarkdownString oldString)
    {
      //TODO: look for diff algorithms
      return newString;
    }
  }
}

