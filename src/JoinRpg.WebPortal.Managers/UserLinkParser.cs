namespace JoinRpg.WebPortal.Managers;

public static class UserLinkParser
{
    public static bool TryParseUserLink(ReadOnlySpan<char> link, out int userId)
    {
        userId = 0;

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

        // Remove /users/ prefix if present
        if (trimmedLink.StartsWith("/users/".AsSpan(), StringComparison.OrdinalIgnoreCase))
        {
            trimmedLink = trimmedLink.Slice(7); // Remove "/users/"
        }

        // Remove trailing slash if present
        trimmedLink = trimmedLink.TrimEnd('/');

        // Now trimmedLink should be just a number
        if (!int.TryParse(trimmedLink, out userId) || userId <= 0)
        {
            userId = 0;
            return false;
        }
        return true;
    }

    public static bool TryParseUserLink(string link, out int userId)
    {
        return TryParseUserLink(link.AsSpan(), out userId);
    }

    public static int ParseUserLink(string link)
    {
        if (!TryParseUserLink(link.AsSpan(), out var userId))
        {
            throw new FormatException($"Неверный формат ссылки на пользователя: '{link}'. Ожидается число, '/users/123' или полный URL.");
        }

        return userId;
    }
}
