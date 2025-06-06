using JoinRpg.DataModel;
using Markdig;
using Microsoft.AspNetCore.Components;
using Vereyon.Web;

namespace JoinRpg.Markdown;

/// <summary>
/// Helper that allows to easily render markdown
/// </summary>
public static class MarkDownRendererFacade
{
    private static readonly MarkdownPipeline markdownPipeline = CreatePipeline();

    private static MarkdownPipeline CreatePipeline()
    {
        return new MarkdownPipelineBuilder()
            .UseSoftlineBreakAsHardlineBreak()
            .UseMediaLinks()
            .UseAutoLinks()
            .UseEntityLinker(["персонаж", "контакты", "группа", "список", "сеткаролей", "экспериментальнаятаблица"])
            .Build();
    }

    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    public static MarkupString ToHtmlString(
        this MarkdownString? markdownString,
        ILinkRenderer? renderer = null)
        => new MarkupString(PerformRender(markdownString,
            renderer,
            (text, writer, pipeline, context) => Markdig.Markdown.ToHtml(text, writer, pipeline, context),
            HtmlSanitizers.Simple));

    /// <summary>
    /// Converts markdown to plain text
    /// </summary>
    public static string ToPlainText(
        this MarkdownString? markdownString,
        ILinkRenderer? renderer = null)
        =>
            PerformRender(markdownString,
                renderer,
                (text, writer, pipeline, context) => Markdig.Markdown.ToPlainText(text, writer, pipeline, context),
                HtmlSanitizers.RemoveAll);

    private static string PerformRender(MarkdownString? markdownString, ILinkRenderer? linkRenderer,
        Action<string, TextWriter, MarkdownPipeline, MarkdownParserContext> renderMethod, IHtmlSanitizer sanitizer)
    {
        if (markdownString?.Contents == null)
        {
            return "";
        }

        var context = new MarkdownParserContext();

        context.Properties.Add(nameof(ILinkRenderer), linkRenderer ?? DoNothingLinkRenderer.Instance);

        var contents = sanitizer.Sanitize(markdownString.Contents);

        var writer = new StringWriter();

        renderMethod(contents, writer, markdownPipeline, context);

        var rendered = writer.ToString();

        return sanitizer.Sanitize(rendered);
    }
}

