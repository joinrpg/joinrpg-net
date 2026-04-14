using JoinRpg.Interfaces;

namespace JoinRpg.Markdown;

/// <summary>
/// Facade for HTML sanitization
/// </summary>
public static class HtmlSanitizeFacade
{
    public static string SanitizeHtml(this TelegramHtmlString str, int maxLength)
    {
        var content = str.Contents.Length > maxLength
            ? str.Contents[..(maxLength - 3)] + "..."
            : str.Contents;
        using var sanitizer = HtmlSanitizers.GetTelegram();
        return sanitizer.Sanitize(content);
    }
}
