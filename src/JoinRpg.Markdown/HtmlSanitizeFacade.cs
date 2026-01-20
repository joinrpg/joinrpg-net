using JoinRpg.Interfaces;

namespace JoinRpg.Markdown;

/// <summary>
/// Facade for HTML sanitization
/// </summary>
public static class HtmlSanitizeFacade
{
    public static string SanitizeHtml(this TelegramHtmlString str)
    {
        using var sanitizer = HtmlSanitizers.GetTelegram();
        return sanitizer.Sanitize(str.Contents);
    }
}
