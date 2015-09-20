using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace JoinRpg.Helpers
{
  public static class StaticStringHelpers
  {
    public static string JoinIfNotNullOrWhitespace([NotNull] this IEnumerable<string> strings, [NotNull] string separator)
    {
      return string.Join(separator, strings.Where(s => !string.IsNullOrWhiteSpace(s)));
    }

    /// <summary>
    /// Return only strings that have specified prefix (and with this prefix removed)
    /// </summary>
    public static IEnumerable<string> SelectWherePrefix([NotNull] this IEnumerable<string> enumerable, [NotNull] string prefix)
    {
      return enumerable.Where(key => key.StartsWith(prefix)).Select(key=> key.Substring(prefix.Length));
    }

    public static IEnumerable<int> UnprefixNumbers(this IEnumerable<string> enumerable, string prefix)
    {
      return enumerable.SelectWherePrefix(prefix).Select(int.Parse);
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
