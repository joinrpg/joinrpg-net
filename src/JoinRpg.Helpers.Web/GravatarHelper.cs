using System;
using System.Security.Cryptography;

namespace JoinRpg.Helpers.Web
{
    public static class GravatarHelper
    {
        /// <summary>
        /// Get link to gravatar.com
        /// </summary>
        public static Uri GetLink(string email, int size)
        {
            static string GravatarHash(string email) => email.ToLowerInvariant().ToHexHash(MD5.Create());

            return new($"https://www.gravatar.com/avatar/{GravatarHash(email)}?d=identicon&s={size}");
        }
    }
}
