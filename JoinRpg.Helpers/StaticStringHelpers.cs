﻿using System;
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

    [NotNull]
    public static string JoinStrings([NotNull] this IEnumerable<char> strings,
      [NotNull] string separator)
    {
      if (strings == null) throw new ArgumentNullException(nameof(strings));
      if (separator == null) throw new ArgumentNullException(nameof(separator));
      return string.Join(separator, strings);
    }

    public static IEnumerable<int> UnprefixNumbers([NotNull, ItemNotNull] this IEnumerable<string> enumerable, [NotNull] string prefix)
    {
      return enumerable.Where(key => key.StartsWith(prefix)).Select(key=> key.Substring(prefix.Length)).Select(int.Parse);
    }

    public static int? UnprefixNumber([NotNull] this string number, [NotNull] string prefix)
    {
      return number.StartsWith(prefix) ? (int?) int.Parse(number.Substring(prefix.Length)) : null;
    }

    [NotNull]
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
      return str.SkipWhile(c => c!=separator).Skip(1).JoinStrings("");
    }

    public static string BeforeSeparator(this string str, char separator)
    {
      return str.TakeWhile(c=> c!=separator).JoinStrings("");
    }

    public static int[] ToIntList([CanBeNull] this string claimIds)
    {
      if (claimIds == null)
      {
        return Array.Empty<int>();
      }
      return claimIds.Split(',').Select(int.Parse).ToArray();
    }

    [NotNull]
    public static IEnumerable<byte> AsUtf8BytesWithLimit([NotNull] string formattableString, int bytesLimit)
    {
      if (formattableString == null) throw new ArgumentNullException(nameof(formattableString));

      var bytes = Encoding.UTF8.GetBytes(formattableString);
      if (bytes.Length < bytesLimit)
      {
        return bytes;
      }
      var epilogue = Encoding.UTF8.GetBytes("...");
      return bytes.Take(bytesLimit - epilogue.Length).Union(epilogue);
    }
  }
}
