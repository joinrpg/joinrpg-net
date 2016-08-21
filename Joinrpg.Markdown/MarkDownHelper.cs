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
    HashSet<string> LinkTypesToMatch { get; }
    string Render(string match, int index, string extra);
  }

  public static class MarkDownHelper
  {
    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    [NotNull]
    public static HtmlString ToHtmlString([CanBeNull] this MarkdownString markdownString, ILinkRenderer renderer = null)
    {
      return markdownString?.Contents == null ? new HtmlString("") : markdownString.RenderMarkDownToHtmlUnsafe(renderer).SanitizeHtml();
    }

    public static string ToPlainText([CanBeNull] this MarkdownString markdownString, ILinkRenderer renderer = null)
    {
      if (markdownString?.Contents == null)
      {
        return null;
      }
      return markdownString.RenderMarkDownToHtmlUnsafe(renderer).RemoveHtml().Trim();
    }

    private static UnSafeHtml RenderMarkDownToHtmlUnsafe(this MarkdownString markdownString, ILinkRenderer renderer)
    {
      //TODO - do we need to save re-use pipeline?
      var pipeline =
        new MarkdownPipelineBuilder().UseSoftlineBreakAsHardlineBreak().UseEntityLinker(renderer).Build();
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
  }
}

