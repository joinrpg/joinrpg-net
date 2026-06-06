using System.Diagnostics.CodeAnalysis;

namespace JoinRpg.WebPortal.Managers.Claims;

public static class UserLinkParser
{
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
