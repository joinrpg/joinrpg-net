// ReSharper disable CheckNamespace
// ReSharper disable IdentifierTypo
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace PscbApi
{
    internal static class CryptoHelper
    {

        public static byte[] Sha256Encode(this IEnumerable<byte> source)
        {
            using (var encoder = new SHA256Managed())
            {
                return encoder.ComputeHash(source.ToArray());
            }
        }

        public static string ToHexString(this byte[] source)
            => string.Join("", source.Select(b => b.ToString("x2")));
    }
}
