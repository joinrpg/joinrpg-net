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

    public static string JoinStrings([NotNull] [ItemNotNull] this IEnumerable<string> strings,
      [NotNull] string separator)
    {
      if (strings == null) throw new ArgumentNullException(nameof(strings));
      if (separator == null) throw new ArgumentNullException(nameof(separator));
      return string.Join(separator, strings);
    }

    /// <summary>
    /// Return only strings that have specified prefix (and with this prefix removed)
    /// </summary>
    public static IEnumerable<string> SelectWherePrefix([NotNull, ItemNotNull] this IEnumerable<string> enumerable, [NotNull] string prefix)
    {
      return enumerable.Where(key => key.StartsWith(prefix)).Select(key=> key.Substring(prefix.Length));
    }

    public static IEnumerable<int> UnprefixNumbers([NotNull, ItemNotNull] this IEnumerable<string> enumerable, [NotNull] string prefix)
    {
      return enumerable.SelectWherePrefix(prefix).Select(int.Parse);
    }

    public static int? UnprefixNumber([NotNull] this string number, [NotNull] string prefix)
    {
      return new[] {number}.UnprefixNumbers(prefix).Select(i => (int?)i).SingleOrDefault();
    }

    public static string AsString(this IEnumerable<string> enumerable)
    {
      return string.Join("", enumerable);
    }

    public static string AsString(this IEnumerable<char> enumerable)
    {
      return string.Join("", enumerable);
    }

    public static string ToHexString(this IEnumerable<byte> bytes)
    {
      return bytes.Select(b => $"{b:x2}").AsString();
    }

    public static string RemoveFromString([NotNull] this string url, [NotNull, ItemNotNull] IEnumerable<string> tokensToRemove)
    {
      if (url == null) throw new ArgumentNullException(nameof(url));
      if (tokensToRemove == null) throw new ArgumentNullException(nameof(tokensToRemove));
      return tokensToRemove.Aggregate(url, (current, replaceToken) => current.Replace(replaceToken, ""));
    }

    public static string ToHexHash(this string str, HashAlgorithm hashAlgorithm)
    {
      var bytes = Encoding.UTF8.GetBytes(str); //TODO: In what encoding Allrpg.info saves passwords? Do not want to think about it....
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

    public static int[] ToIntList([CanBeNull] this string claimIds)
    {
      if (claimIds == null)
      {
        return Array.Empty<int>();
      }
      return claimIds.Split(',').Select(int.Parse).ToArray();
    }
  }
}
