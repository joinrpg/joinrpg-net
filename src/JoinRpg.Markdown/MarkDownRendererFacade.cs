using JoinRpg.DataModel;
using Markdig;
using Microsoft.AspNetCore.Components;

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
            .UsePipeTables()
            .UseEntityLinker(["персонаж", "контакты", "группа", "список", "сеткаролей", "экспериментальнаятаблица"])
            .Build();
    }

    /// <summary>
    /// Converts markdown to HtmlString with all sanitization
    /// </summary>
    public static MarkupString ToHtmlString(
        this MarkdownString? markdownString,
        ILinkRenderer? renderer = null)
    {
        using var sanitizer = HtmlSanitizers.GetSimple();
        return new MarkupString(sanitizer.Sanitize(PerformRender(markdownString, renderer, Markdig.Markdown.ToHtml)));
    }

    /// <summary>
    /// Превращает Markdown в MarkupString, который можно вывести без дополнительного Escape HTML
    /// </summary>
    public static MarkupString ToPlainTextAndEscapeHtml(
        this MarkdownString? markdownString,
        ILinkRenderer? renderer = null)
    {
        using var sanitizer = HtmlSanitizers.GetRemoveAll();
        return new MarkupString(sanitizer.Sanitize(PerformRender(markdownString, renderer, Markdig.Markdown.ToPlainText)));
    }

    [Obsolete("Выберите ToPlainTextAndEscapeHtml/ToPlainTextWithoutHtmlEscape вместо этого")]
    public static string ToPlainText(
        this MarkdownString? markdownString,
        ILinkRenderer? renderer = null)
    {
        using var sanitizer = HtmlSanitizers.GetRemoveAll();
        return sanitizer.Sanitize(PerformRender(markdownString, renderer, Markdig.Markdown.ToPlainText));
    }

    /// <summary>
    /// Превращает Markdown в строку, которую можно использовать только при гарантии, что ее не будут трактовать как HTML. При выводе в шаблон
    /// ее необходимо будет экранировать (escape), но как правило это автоматически происходит
    /// </summary>
    public static string ToPlainTextWithoutHtmlEscape(
    this MarkdownString? markdownString,
    ILinkRenderer? renderer = null)
    {
        var result = PerformRender(markdownString, renderer, Markdig.Markdown.ToPlainText);
        if (result.Length > 0 && result[^1] == '\n')
        {
            return result[..^1];
        }
        return result;
    }

    private static string PerformRender(MarkdownString? markdownString, ILinkRenderer? linkRenderer,
        Func<string, MarkdownPipeline, MarkdownParserContext, string> renderMethod)
    {
        if (markdownString?.Contents == null)
        {
            return "";
        }

        var context = new MarkdownParserContext();

        context.Properties.Add(nameof(ILinkRenderer), linkRenderer ?? DoNothingLinkRenderer.Instance);

        return renderMethod(markdownString.Contents, markdownPipeline, context);

    }
}

