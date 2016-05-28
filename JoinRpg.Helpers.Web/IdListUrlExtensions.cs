using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Helpers.Web
{
  public static class IdListUrlExtensions
  {
    public static string CompressIdList(this IEnumerable<int> list)
    {
      return list.OrderBy(l => l).DeltaCompress().Compress1().Join("_");
    }

    public static IEnumerable<int> UnCompressIdList(this string compressedList)
    {
      return compressedList.Split('_').UnCompress1().DeltaUnCompress();
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
