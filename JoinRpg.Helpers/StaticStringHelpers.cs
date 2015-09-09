using System.Collections.Generic;
using System.Linq;

namespace JoinRpg.Helpers
{
  public static class StaticStringHelpers
  {
    public static string JoinIfNotNullOrWhitespace(this IEnumerable<string> strings, string separator)
    {
      return string.Join(separator, strings.Select(string.IsNullOrWhiteSpace));
    }
  }
}
