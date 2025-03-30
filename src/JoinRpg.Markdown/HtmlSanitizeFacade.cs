using Microsoft.AspNetCore.Components;

namespace JoinRpg.Markdown;

/// <summary>
/// Facade for HTML sanitization
/// </summary>
public static class HtmlSanitizeFacade
{
    /// <summary>
    /// Sanitize all Html, leaving safe subset
    /// </summary>
    /// <returns>We are returning <see cref="MarkupString"/> to signal "no need to sanitize this again"</returns>
    public static MarkupString SanitizeHtml(this string str) => new MarkupString(HtmlSanitizers.Simple.Sanitize(str));
}
