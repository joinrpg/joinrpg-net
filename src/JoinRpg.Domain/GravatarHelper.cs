using System.Security.Cryptography;
using System.Text;

namespace JoinRpg.Domain;

/// <summary>
/// Ссылка на Gravatar. Публичный, потому что её строят и мимо EF:
/// сырой SQL в Tools.RestoreLostUsers должен заводить ту же аватарку, что и регистрация.
/// </summary>
public static class GravatarHelper
{
    /// <summary>
    /// Размер, с которым аватарка по умолчанию заводится пользователю.
    /// </summary>
    public const int DefaultAvatarSize = 64;

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
