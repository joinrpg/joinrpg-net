// ReSharper disable CheckNamespace
namespace PscbApi
{
    internal static class StringExtensions
    {
        public static string TrimLength(this string source, int maxLength)
            => string.IsNullOrWhiteSpace(source) || source.Length < maxLength
                ? source
                : source.Substring(0, maxLength);
    }
}
