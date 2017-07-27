using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
  public static class StaticStringHelpers
  {
    [NotNull]
    public static string JoinIfNotNullOrWhitespace([NotNull, ItemCanBeNull] this IEnumerable<string> strings, [NotNull] string separator)
    {
      return string.Join(separator, strings.WhereNotNullOrWhiteSpace());
    }

    [NotNull, ItemNotNull]
    public static IEnumerable<string> WhereNotNullOrWhiteSpace([ItemCanBeNull] [NotNull] this IEnumerable<string> strings)
    {
      if (strings == null) throw new ArgumentNullException(nameof(strings));
      return strings.Where(s => !string.IsNullOrWhiteSpace(s));
    }

    [NotNull]
    public static string JoinStrings([NotNull] [ItemNotNull] this IEnumerable<string> strings,
      [NotNull] string separator)
    {
      if (strings == null) throw new ArgumentNullException(nameof(strings));
      if (separator == null) throw new ArgumentNullException(nameof(separator));
      return string.Join(separator, strings);
    }

    [NotNull, PublicAPI]
    public static string AsString([NotNull] this IEnumerable<char> strings)
    {
      if (strings == null) throw new ArgumentNullException(nameof(strings));
      return string.Join("", strings);
    }

    public static IEnumerable<int> UnprefixNumbers([NotNull, ItemNotNull] this IEnumerable<string> enumerable, [NotNull] string prefix)
    {
      return enumerable.Where(key => key.StartsWith(prefix)).Select(key=> key.Substring(prefix.Length)).Select(int.Parse);
    }

    public static int? UnprefixNumber([NotNull] this string number, [NotNull] string prefix)
    {
      return number.StartsWith(prefix) ? (int?) int.Parse(number.Substring(prefix.Length)) : null;
    }

    [NotNull, PublicAPI]
    public static string ToHexString([NotNull] this IEnumerable<byte> bytes)
    {
      if (bytes == null) throw new ArgumentNullException(nameof(bytes));
      return bytes.Select(b => $"{b:x2}").JoinStrings("");
    }

    [NotNull]
    public static byte[] FromHexString ([NotNull] this string str)
    {
      if (str == null) throw new ArgumentNullException(nameof(str));

      str = str.Trim();
      if (str.Length%2 == 1)
      {
        str = "0" + str;
      }
      var result = new byte[str.Length/2];
      for (int i = 0; i < str.Length; i += 2)
      {
        result[i / 2] = (byte) (HexCharToInt(str[i + 1]) + HexCharToInt(str[i]) * 16);
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

    [NotNull]
    public static string RemoveFromString([NotNull] this string url, [NotNull, ItemNotNull] IEnumerable<string> tokensToRemove, StringComparison stringComparison = StringComparison.CurrentCulture)
    {
      if (url == null) throw new ArgumentNullException(nameof(url));
      if (tokensToRemove == null) throw new ArgumentNullException(nameof(tokensToRemove));
      return tokensToRemove.Aggregate(url, (current, replaceToken) => current.RemoveFromString(replaceToken, stringComparison));
    }

    [NotNull, PublicAPI]
    public static string RemoveFromString([NotNull] this string str, [NotNull] string tokenToRemove, StringComparison stringComparison = StringComparison.CurrentCulture)
    {
      if (str == null) throw new ArgumentNullException(nameof(str));
      if (tokenToRemove == null) throw new ArgumentNullException(nameof(tokenToRemove));

      string retval = str;
      // find the first occurence of oldValue
      int pos = retval.IndexOf(tokenToRemove, stringComparison);

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

    public static string AfterSeparator(this string str, char separator)
    {
      return str.SkipWhile(c => c!=separator).Skip(1).AsString();
    }

    public static string BeforeSeparator(this string str, char separator)
    {
      return str.TakeWhile(c=> c!=separator).AsString();
    }

    [NotNull]
    public static int[] ToIntList([CanBeNull] this string claimIds)
    {
      if (string.IsNullOrWhiteSpace(claimIds))
      {
        return Array.Empty<int>();
      }
      return claimIds.Split(',').WhereNotNullOrWhiteSpace().Select(int.Parse).ToArray();
    }

    [NotNull]
    public static IEnumerable<byte> AsUtf8BytesWithLimit([NotNull] this string formattableString, int bytesLimit)
    {
      if (formattableString == null) throw new ArgumentNullException(nameof(formattableString));

      var ch = formattableString.ToCharArray();
      var epilogue = Encoding.UTF8.GetBytes("…");
      for (var i = ch.Length; i > 0; i--)
      {
        var chunkBytes = Encoding.UTF8.GetByteCount(ch, 0, i);
        if (chunkBytes +  epilogue.Length <= bytesLimit)
        {
          return Encoding.UTF8.GetBytes(ch, 0, i).Concat(epilogue);
        }
      }
      
      return Enumerable.Empty<byte>();
    }

    [NotNull]
    public static string WithDefaultStringValue([CanBeNull] this string value, [NotNull] string defaultValue)
    {
      if (defaultValue == null) throw new ArgumentNullException(nameof(defaultValue));
      return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    [MustUseReturnValue]
    public static string ToHexString(this Guid guid) => guid.ToByteArray().ToHexString();
  }
}
