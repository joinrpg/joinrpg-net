using System.Security.Cryptography;
using System.Text;

namespace JoinRpg.Helpers;

public static class StaticStringHelpers
{
    public static string JoinIfNotNullOrWhitespace(
        this IEnumerable<string?> strings,
        string separator) => string.Join(separator, strings.WhereNotNullOrWhiteSpace());

    public static IEnumerable<string> WhereNotNullOrWhiteSpace(
        this IEnumerable<string?> strings)
    {
        if (strings == null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        return strings.Where(s => !string.IsNullOrWhiteSpace(s)).WhereNotNull();
    }

    public static string JoinStrings(
        this IEnumerable<string> strings,
        string separator)
    {
        if (strings == null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        if (separator == null)
        {
            throw new ArgumentNullException(nameof(separator));
        }

        return string.Join(separator, strings);
    }

    public static string AsString(
        this IEnumerable<char> strings)
    {
        if (strings == null)
        {
            throw new ArgumentNullException(nameof(strings));
        }

        return string.Join("", strings);
    }

    public static IEnumerable<int> UnprefixNumbers(
        this IEnumerable<string> enumerable,
        string prefix)
    {
        return enumerable.Where(key => key.StartsWith(prefix))
            .Select(key => key.Substring(prefix.Length)).Select(int.Parse);
    }

    public static int? UnprefixNumber(
        this string number,
        string prefix)
    {
        return number.StartsWith(prefix)
            ? int.Parse(number.Substring(prefix.Length))
            : null;
    }

    public static string ToHexString(
        this IEnumerable<byte> bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return bytes.Select(b => $"{b:x2}").JoinStrings("");
    }

    public static byte[] FromHexString(
        this string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        str = str.Trim();
        if (str.Length % 2 == 1)
        {
            str = "0" + str;
        }

        var result = new byte[str.Length / 2];
        for (var i = 0; i < str.Length; i += 2)
        {
            result[i / 2] = (byte)(HexCharToInt(str[i + 1]) + HexCharToInt(str[i]) * 16);
        }

        return result;
    }

    private static int HexCharToInt(char ch)
    {
        if (char.IsDigit(ch))
        {
            return ch - '0';
        }

        ch = char.ToLowerInvariant(ch);
        if (ch >= 'a' && ch <= 'f')
        {
            return ch - 'a' + 10;
        }

        throw new ArgumentException(nameof(ch));
    }

    public static string RemoveFromString(
        this string url,
        IEnumerable<string> tokensToRemove,
        StringComparison stringComparison = StringComparison.CurrentCulture)
    {
        if (url == null)
        {
            throw new ArgumentNullException(nameof(url));
        }

        if (tokensToRemove == null)
        {
            throw new ArgumentNullException(nameof(tokensToRemove));
        }

        return tokensToRemove.Aggregate(url,
            (current, replaceToken) =>
                current.RemoveFromString(replaceToken, stringComparison));
    }

    public static string RemoveFromString(
        this string str,
        string tokenToRemove,
        StringComparison stringComparison = StringComparison.CurrentCulture)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (tokenToRemove == null)
        {
            throw new ArgumentNullException(nameof(tokenToRemove));
        }

        var retval = str;
        // find the first occurence of oldValue
        var pos = retval.IndexOf(tokenToRemove, stringComparison);

        while (pos > -1)
        {
            // remove oldValue from the string
            retval = retval.Remove(pos, tokenToRemove.Length);

            // check if oldValue is found further down
            pos = retval.IndexOf(tokenToRemove, pos, stringComparison);
        }

        return retval;
    }

    public static string ToHexHash(this string str, HashAlgorithm hashAlgorithm)
    {
        var bytes = Encoding.UTF8.GetBytes(str);
        return hashAlgorithm.ComputeHash(bytes).ToHexString();
    }

    public static int[] ToIntList(this string? claimIds)
    {
        if (claimIds is null)
        {
            return Array.Empty<int>();
        }

        return claimIds.Split(',').WhereNotNullOrWhiteSpace().Select(int.Parse).ToArray();
    }

    public static string WithDefaultStringValue(
        this string? value,
        string defaultValue)
    {
        if (defaultValue == null)
        {
            throw new ArgumentNullException(nameof(defaultValue));
        }

        // string.IsNullOrEmpty used from netstandard2, and here is not annotated yet.
        return (value is null || string.IsNullOrEmpty(value)) ? defaultValue : value;
    }

    public static string ToHexString(this Guid guid) => guid.ToByteArray().ToHexString();
}
