using System;
using System.Web;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.Helpers.Web;
using Markdig;

namespace Joinrpg.Markdown
{
    /// <summary>
    /// Helper that allows to easily render markdown
    /// </summary>
    public static class MarkDownRendererFacade
    {
        /// <summary>
        /// Converts markdown to HtmlString with all sanitization
        /// </summary>
        [NotNull]
        public static IHtmlString ToHtmlString([CanBeNull] this MarkdownString markdownString,
            [NotNull] ILinkRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            return markdownString?.Contents == null
                ? new HtmlString("")
                : markdownString.RenderMarkDownToHtmlUnsafe(renderer).SanitizeHtml();
        }

        /// <summary>
        /// Converts markdown to HtmlString with all sanitization
        /// </summary>
        [NotNull]
        public static IHtmlString ToHtmlString([CanBeNull]
            this MarkdownString markdownString) =>
            markdownString.ToHtmlString(DoNothingLinkRenderer.Instance);

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        [NotNull]
        public static IHtmlString ToPlainText([CanBeNull] this MarkdownString markdownString,
            [NotNull] ILinkRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            if (markdownString?.Contents == null)
            {
                return new HtmlString("");
            }
            return markdownString.RenderMarkDownToPlainTextUnsafe(renderer).RemoveHtml();
        }

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        [NotNull]
        public static IHtmlString ToPlainText([CanBeNull]
            this MarkdownString markdownString) =>
            markdownString.ToPlainText(DoNothingLinkRenderer.Instance);


        private static UnSafeHtml RenderMarkDownToHtmlUnsafe(this MarkdownString markdownString,
            [NotNull] ILinkRenderer renderer)
        {
            var pipeline = ConstructPipelineWithRenderer(renderer);
            return Markdig.Markdown.ToHtml(markdownString.Contents, pipeline);
        }

        private static UnSafeHtml RenderMarkDownToPlainTextUnsafe(
            this MarkdownString markdownString,
            [NotNull] ILinkRenderer renderer)
        {
            var pipeline = ConstructPipelineWithRenderer(renderer);
            return Markdig.Markdown.ToPlainText(markdownString.Contents, pipeline);
        }

        private static MarkdownPipeline ConstructPipelineWithRenderer(ILinkRenderer renderer)
        {
            if (renderer == null) throw new ArgumentNullException(nameof(renderer));
            //TODO - do we need to save re-use pipeline?
            return new MarkdownPipelineBuilder()
                .UseSoftlineBreakAsHardlineBreak()
                .UseAutoLinks()
                .UseEntityLinker(renderer)
                .Build();
        }
    }
}

