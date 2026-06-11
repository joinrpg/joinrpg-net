using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.WebPortal.Managers.Claims;

public abstract record ParsedUserLink
{
    public sealed record JoinRpgUser(UserIdentification UserId) : ParsedUserLink;
    public sealed record VkProfile(string VkId) : ParsedUserLink;
    public sealed record TelegramProfile(string Username) : ParsedUserLink;
    public sealed record EmailAddress(string Email) : ParsedUserLink;
}

public static class UserLinkParser
{
    public static bool TryParseSocialUserLink(string link, [NotNullWhen(true)] out ParsedUserLink? result)
    {
        result = null;
        var trimmed = link.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            return false;
        }

        if (TryParseVkLink(trimmed, out var vkId))
        {
            result = new ParsedUserLink.VkProfile(vkId!);
            return true;
        }

        if (TryParseTelegramLink(trimmed, out var username))
        {
            result = new ParsedUserLink.TelegramProfile(username!);
            return true;
        }

        if (TryParseEmailLink(trimmed, out var email))
        {
            result = new ParsedUserLink.EmailAddress(email!);
            return true;
        }

        if (TryParseUserLink(trimmed.AsSpan(), out var userId))
        {
            result = new ParsedUserLink.JoinRpgUser(userId);
            return true;
        }

        return false;
    }

    private static bool TryParseVkLink(string link, [NotNullWhen(true)] out string? vkId)
    {
        vkId = null;
        var span = link.AsSpan();

        if (span.StartsWith("https://".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var protocolEnd = span.IndexOf("://".AsSpan(), StringComparison.Ordinal);
            span = span[(protocolEnd + 3)..];
        }

        const string vkComPrefix = "vk.com/";
        const string vkRuPrefix = "vk.ru/";

        if (span.StartsWith(vkComPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            span = span[vkComPrefix.Length..];
        }
        else if (span.StartsWith(vkRuPrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            span = span[vkRuPrefix.Length..];
        }
        else
        {
            return false;
        }

        span = span.TrimEnd('/');
        if (span.IsEmpty)
        {
            return false;
        }

        vkId = span.ToString();
        return true;
    }

    private static bool TryParseTelegramLink(string link, [NotNullWhen(true)] out string? username)
    {
        username = null;
        var span = link.AsSpan().Trim();

        if (span.StartsWith("@".AsSpan(), StringComparison.Ordinal))
        {
            var name = span[1..].Trim();
            if (name.IsEmpty) return false;
            username = name.ToString();
            return true;
        }

        if (span.StartsWith("https://".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            span.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            var protocolEnd = span.IndexOf("://".AsSpan(), StringComparison.Ordinal);
            span = span[(protocolEnd + 3)..];
        }

        const string tMePrefix = "t.me/";
        if (!span.StartsWith(tMePrefix.AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        span = span[tMePrefix.Length..].TrimEnd('/');
        if (span.IsEmpty)
        {
            return false;
        }

        username = span.ToString();
        return true;
    }

    private static bool TryParseEmailLink(string link, [NotNullWhen(true)] out string? email)
    {
        email = null;
        var atIndex = link.IndexOf('@');
        if (atIndex <= 0)
        {
            return false;
        }

        if (link.Contains('/') || link.Contains(':'))
        {
            return false;
        }

        if (atIndex == link.Length - 1)
        {
            return false;
        }

        email = link;
        return true;
    }


    public static bool TryParseUserLink(ReadOnlySpan<char> link, [NotNullWhen(true)] out UserIdentification? userId)
    {
        userId = null;

        var trimmedLink = link.Trim();

        if (trimmedLink.IsEmpty)
        {
            return false;
        }

        // Remove protocol and domain if present
        if (trimmedLink.StartsWith("http://".AsSpan(), StringComparison.OrdinalIgnoreCase) ||
            trimmedLink.StartsWith("https://".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            // Find the :// to skip protocol
            var protocolEnd = trimmedLink.IndexOf("://".AsSpan(), StringComparison.Ordinal);
            if (protocolEnd >= 0)
            {
                trimmedLink = trimmedLink[(protocolEnd + 3)..]; // Skip "://"

                // Find the first slash after domain
                var slashIndex = trimmedLink.IndexOf('/');
                if (slashIndex >= 0)
                {
                    trimmedLink = trimmedLink.Slice(slashIndex); // Keep the slash and path
                }
                else
                {
                    return false; // URL without path
                }
            }
        }

        // Remove /user/ prefix if present
        if (trimmedLink.StartsWith("/user/".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            trimmedLink = trimmedLink.Slice(6); // Remove "/user/"
        }

        // Remove trailing slash if present
        trimmedLink = trimmedLink.TrimEnd('/');

        // Now trimmedLink should be just a number
        if (!UserIdentification.TryParse(trimmedLink, null, out userId))
        {
            userId = null;
            return false;
        }
        return true;
    }

    public static UserIdentification ParseUserLink(string link)
    {
        if (!TryParseUserLink(link.AsSpan(), out var userId))
        {
            throw new FormatException($"Неверный формат ссылки на пользователя: '{link}'. Ожидается число, '/user/123' или полный URL.");
        }

        return userId;
    }
}
