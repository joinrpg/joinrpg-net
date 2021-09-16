// ReSharper disable CheckNamespace

using System.Text;

namespace PscbApi
{
    internal static class StringExtensions
    {
        public static string TrimLength(this string source, int maxLength)
            => string.IsNullOrWhiteSpace(source) || source.Length < maxLength
                ? source
                : source.Substring(0, maxLength);

        public static byte[] ToUtf8Bytes(this string source)
            => Encoding.Convert(
                Encoding.Unicode,
                Encoding.UTF8,
                Encoding.Unicode.GetBytes(source));


    }
}
