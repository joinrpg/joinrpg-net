﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using JetBrains.Annotations;

namespace JoinRpg.Helpers.Web
{
  public static class IdListUrlExtensions
  {
    public static string CompressIdList(this IEnumerable<int> list)
    {
      return list.OrderBy(l => l).DeltaCompress().MagicJoin();
    }

    public static IReadOnlyCollection<int> UnCompressIdList(this string compressedList)
    {
      return compressedList.MagicUnJoin().DeltaUnCompress().ToList();
    }

    private static IEnumerable<int> MagicUnJoin(this string str)
    {
      var idx = -1;
      string buffer = "";
      while (idx + 1 < str.Length)
      {
        idx++;
        if (char.IsDigit(str[idx]))
        {
          buffer += str[idx];
          continue;
        }
        if (buffer != "")
        {
          yield return int.Parse(buffer) + 25;
          buffer = "";
        }
        if (str[idx] >= 'a' && str[idx] <= 'z')
        {
          yield return (str[idx] - 'a' + 2);
        }
        if (str[idx] >= 'A' && str[idx] <= 'Z')
        {
          for (char c = 'A'; c <= str[idx]; c++)
          {
            yield return 1;
          }
        }
      }
      if (buffer != "")
      {
        yield return int.Parse(buffer) + 25;
      }
    }
    private static string MagicJoin([NotNull] this IEnumerable<int> list)
    {
      if (list == null) throw new ArgumentNullException(nameof(list));
      StringBuilder builder = new StringBuilder();
      using (var enumerator = list.GetEnumerator())
      {
        bool needSep = false;
        while (enumerator.MoveNext())
        {
          var next = enumerator.Current;
          while (true)
          {
            if (next == 1)
            {
              var count = 1;
              var b = enumerator.MoveNext();
              while (b && enumerator.Current == 1 && count < 25)
              {
                count++;
                b = enumerator.MoveNext();
              }
              builder.Append((char) ('A' + count - 1));
              if (!b)
              {
                break;
              }
              next = enumerator.Current;
              needSep = false;
              continue;
            }
            if (next < 25)
            {
              builder.Append((char) ('a' + next - 2));
              needSep = false;
              break;
            }

            if (needSep)
            {
              builder.Append(",");
            }
            builder.Append(next -25);
            needSep = true;
            break;
          }
        }
      }
      return builder.ToString();
    }

    private static IEnumerable<int> DeltaCompress(this IEnumerable<int> list)
    {
      var prev = 0;
      foreach (var i in list)
      {
        yield return i - prev;
        prev = i;
      }
    }

    private static IEnumerable<int> DeltaUnCompress(this IEnumerable<int> list)
    {
      var prev = 0;
      foreach (var i in list)
      {
        prev += i;
        yield return prev;
      }
    }
  }
}
