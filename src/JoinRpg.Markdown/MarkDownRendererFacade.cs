using System;
using System.IO;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers.Web;
using Markdig;
using Vereyon.Web;

namespace JoinRpg.Markdown
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
            this MarkdownString? markdownString,
            ILinkRenderer? renderer = null)
            => PerformRender(markdownString,
                renderer,
                (text, writer, pipeline, context) => Markdig.Markdown.ToHtml(text, writer, pipeline, context),
                HtmlSanitizers.Simple);

        /// <summary>
        /// Converts markdown to plain text
        /// </summary>
        [NotNull]
        public static JoinHtmlString ToPlainText([CanBeNull]
            this MarkdownString? markdownString,
            ILinkRenderer? renderer = null)
            =>
                PerformRender(markdownString,
                    renderer,
                    (text, writer, pipeline, context) => Markdig.Markdown.ToPlainText(text, writer, pipeline, context),
                    HtmlSanitizers.RemoveAll);

        private static JoinHtmlString PerformRender(MarkdownString? markdownString, ILinkRenderer? linkRenderer,
            Action<string, TextWriter, MarkdownPipeline, MarkdownParserContext> renderMethod, IHtmlSanitizer sanitizer)
        {
            linkRenderer ??= DoNothingLinkRenderer.Instance;
            if (markdownString?.Contents == null)
            {
                return "".MarkAsHtmlString();
            }

            var context = new MarkdownParserContext();

            var contents = sanitizer.Sanitize(markdownString.Contents);

            //TODO - do we need to save re-use pipeline?
            var pipeline = new MarkdownPipelineBuilder()
                .UseSoftlineBreakAsHardlineBreak()
                .UseMediaLinks()
                .UseAutoLinks()
                .UseEntityLinker(linkRenderer)
                .Build();

            var writer = new StringWriter();

            renderMethod(contents, writer, pipeline, context);

            var rendered = writer.ToString();

            return sanitizer.Sanitize(rendered).MarkAsHtmlString();
        }
    }
}

