using System.Security.Cryptography;
using System.Text;

namespace JoinRpg.Services.Impl;

public static class GravatarHelper
{
    /// <summary>
    /// Get link to gravatar.com
    /// </summary>
    public static Uri GetLink(string email, int size)
    {
        ArgumentNullException.ThrowIfNull(email);

        var bytes = Encoding.UTF8.GetBytes(email.ToLowerInvariant());
        var gravatarHash = Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();
        var defaultMode = "robohash";

        return new($"https://www.gravatar.com/avatar/{gravatarHash}?d={defaultMode}&s={size}");
    }
}
