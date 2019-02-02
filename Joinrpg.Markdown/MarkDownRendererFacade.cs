using System;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;
using Markdig;
using Vereyon.Web;

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
        public static JoinHtmlString ToHtmlString([CanBeNull]
            this MarkdownString markdownString,
            ILinkRenderer renderer = null)
            => PerformRender(markdownString,
                renderer,
                Markdig.Markdown.ToHtml,
                HtmlSanitizers.Simple);

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        [NotNull]
        public static JoinHtmlString ToPlainText([CanBeNull]
            this MarkdownString markdownString,
            ILinkRenderer renderer = null)
            =>
                PerformRender(markdownString,
                    renderer,
                    Markdig.Markdown.ToPlainText,
                    HtmlSanitizers.RemoveAll);

        private static JoinHtmlString PerformRender(MarkdownString markdownString, ILinkRenderer linkRenderer,
            Func<string, MarkdownPipeline, string> renderMethod, IHtmlSanitizer sanitizer)
        {
            linkRenderer = linkRenderer ?? DoNothingLinkRenderer.Instance;
            if (markdownString?.Contents == null)
            {
                return "".MarkAsHtmlString();
            }

            var contents = sanitizer.Sanitize(markdownString.Contents);


            //TODO - do we need to save re-use pipeline?
            var pipeline = new MarkdownPipelineBuilder()
                .UseSoftlineBreakAsHardlineBreak()
                .UseAutoLinks()
                .UseEntityLinker(linkRenderer)
                .Build();

            return sanitizer.Sanitize(renderMethod(contents, pipeline)).MarkAsHtmlString();
        }
    }
}

