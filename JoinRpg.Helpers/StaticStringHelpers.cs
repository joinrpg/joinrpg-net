using System;
using System.Collections.Generic;
using System.Linq;
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

    public static string Join([NotNull, ItemNotNull] this IEnumerable<string> strings, [NotNull] string separator)
    {
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
  }
}
