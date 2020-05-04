using System.Security.Cryptography;

namespace JoinRpg.Helpers.Web
{
    public static class GravatarExtensions
    {
        public static string GravatarHash(this string email) => email.ToLowerInvariant().ToHexHash(MD5.Create());
    }
}
