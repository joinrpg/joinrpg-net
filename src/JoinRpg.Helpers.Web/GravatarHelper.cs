using System.Security.Cryptography;
using System.Text;

namespace JoinRpg.Helpers.Web;

public static class GravatarHelper
{
    /// <summary>
    /// Get link to gravatar.com
    /// </summary>
    public static Uri GetLink(string email, int size)
    {
        ArgumentNullException.ThrowIfNull(email);
        static string GravatarHash(string email)
        {
            var bytes = Encoding.UTF8.GetBytes(email.ToLowerInvariant());
            return Convert.ToHexString(MD5.HashData(bytes));
        }

        return new($"https://www.gravatar.com/avatar/{GravatarHash(email)}?d=identicon&s={size}");
    }
}
