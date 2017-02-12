using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace JoinRpg.Helpers.Web
{
  public static class IdListUrlExtensions
  {
    public static string CompressIdList(this IEnumerable<int> list)
    {
      return list.OrderBy(l => l).DeltaCompress().Compress1().CompressLongEmpty().MagicJoin();
    }

    public static IReadOnlyCollection<int> UnCompressIdList(this string compressedList)
    {
      return compressedList.MagicUnJoin().UnCompressLongEmpty().UnCompress1().DeltaUnCompress().ToList();
    }

    private static IEnumerable<string> MagicUnJoin(this string str)
    {
      var idx = -1;
      string buffer = "";
      while (idx + 1 < str.Length)
      {
        idx++;
        if (str[idx] >= 'a' && str[idx] <= 'z')
        {
          yield return (str[idx] - 'a').ToString();
          continue;
        }
        if (str[idx] >= 'A' && str[idx] <= 'Z')
        {
          yield return str[idx].ToString();
          continue;
        }
        if (str[idx] == '_')
        {
          yield return buffer;
          buffer = "";
          continue;
        }
        buffer += str[idx];
      }
      if (buffer != "")
      {
        yield return buffer;
      }
    }
    private static string MagicJoin(this IEnumerable<string> list)
    {
      StringBuilder builder = new StringBuilder();
      foreach (var str in list)
      {
        if (str.Length == 1 && str[0] >= '0' && str[0] <= '9')
        {
          builder.Append((char) ('a' + str[0] - '0'));
        } else if (str.Length == 1)
        {
          builder.Append(str);
        }
        else
        {
          builder.Append(str);
          builder.Append("_");
        }
      }
      return builder.ToString();
    }

    private static IEnumerable<string> CompressLongEmpty(this IEnumerable<string> list)
    {
      var count = 0;
      var maxCompressLength = 'Z' - 'A';
      foreach (var i in list)
      {
        if (i != "")
        {
          switch (count)
          {
            case 0:
              break;
            case 1:
              yield return "";
              break;
            default:
              yield return OutputChar(count);
              break;
          }
          count = 0;
          yield return i;
        }
        else
        {
          count++;
          if (count == maxCompressLength)
          {
            yield return OutputChar(count);
            count = 0;
          }
        }
      }

      switch (count)
      {
        case 0:
          break;
        case 1:
          yield return "";
          break;
        default:
          yield return OutputChar(count);
          break;
      }
    }

    private static string OutputChar(int count)
    {
      return ((char) ('A' + count - 2)).ToString();
    }

    private static IEnumerable<string> UnCompressLongEmpty(this IEnumerable<string> list)
    {

      foreach (var str in list)
      {
        if (str.Length == 1 && (str[0] >= 'A' && str[0] <= 'Z'))
        {
          var count = str[0] - 'A' + 2;
          for (var j = 0; j < count; j++)
          {
            yield return "";
          }
        }
        else
        {
          yield return str;
        }
      }
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


    private static IEnumerable<string> Compress1(this IEnumerable<int> list)
    {
      return list.Select(i => i == 1 ? "" : i.ToString());
    }

    private static IEnumerable<int> UnCompress1(this IEnumerable<string> list)
    {
      return list.Select(i => i == "" ? 1 : int.Parse(i));
    }
  }
}
