using JoinRpg.Interfaces;

namespace JoinRpg.Markdown;

/// <summary>
/// Facade for HTML sanitization
/// </summary>
public static class HtmlSanitizeFacade
{
    private const int TelegramMaxMessageLength = 4096;

    public static string SanitizeHtml(this TelegramHtmlString str)
    {
        using var sanitizer = HtmlSanitizers.GetTelegram();
        var result = sanitizer.Sanitize(str.Contents);
        return TruncateHtml(result, TelegramMaxMessageLength);
    }

    internal static string TruncateHtml(string html, int maxLength)
    {
        if (html.Length <= maxLength)
        {
            return html;
        }

        const string suffix = "...";
        int limit = maxLength - suffix.Length;

        // Find cut position: not inside an HTML tag
        int cutPos = limit;
        int lastOpen = cutPos > 0 ? html.LastIndexOf('<', cutPos - 1) : -1;
        int lastClose = cutPos > 0 ? html.LastIndexOf('>', cutPos - 1) : -1;

        if (lastOpen > lastClose)
        {
            // We're inside a tag, cut before it
            cutPos = lastOpen;
        }

        var truncatedPart = html[..cutPos];
        var closingTags = GetUnclosedTagsInReverse(truncatedPart);
        var closingTagsStr = string.Join("", closingTags.Select(t => $"</{t}>"));

        // If closing tags would push us over the limit, cut earlier
        var totalLength = cutPos + suffix.Length + closingTagsStr.Length;
        if (totalLength > maxLength)
        {
            var adjustedLimit = maxLength - suffix.Length - closingTagsStr.Length;
            lastOpen = adjustedLimit > 0 ? html.LastIndexOf('<', adjustedLimit - 1) : -1;
            lastClose = adjustedLimit > 0 ? html.LastIndexOf('>', adjustedLimit - 1) : -1;
            cutPos = lastOpen > lastClose ? lastOpen : adjustedLimit;
            truncatedPart = html[..cutPos];
        }

        return truncatedPart + suffix + closingTagsStr;
    }

    private static Stack<string> GetUnclosedTagsInReverse(string html)
    {
        var openTags = new Stack<string>();
        int i = 0;
        while (i < html.Length)
        {
            if (html[i] == '<')
            {
                int end = html.IndexOf('>', i);
                if (end < 0)
                {
                    break;
                }

                var tag = html[(i + 1)..end].Trim();
                if (tag.StartsWith('/'))
                {
                    var tagName = tag[1..].Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
                    if (openTags.Count > 0 && openTags.Peek() == tagName)
                    {
                        openTags.Pop();
                    }
                }
                else if (!tag.EndsWith('/'))
                {
                    var tagName = tag.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].ToLowerInvariant();
                    openTags.Push(tagName);
                }

                i = end + 1;
            }
            else
            {
                i++;
            }
        }

        return openTags; // Stack iterates LIFO — innermost tag first = correct closing order
    }
}
