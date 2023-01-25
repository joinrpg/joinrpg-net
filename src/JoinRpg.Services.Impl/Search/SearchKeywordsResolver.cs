using System.Text.RegularExpressions;

namespace JoinRpg.Services.Impl.Search;

internal static class SearchKeywordsResolver
{
    public static (int? Id, bool whenFoundItIsPerfectMatch) TryGetId(
      string searchString,
      string[] keysForPerfectMath)
    {
        // bare number in search string requires, among other, search by id. No perfect match.
        if (int.TryParse(searchString.Trim(), out var entityId))
        {
            return (entityId, false);
        }

        //"%контакты4196", "персонаж4196", etc provide a perfect match
        if (keysForPerfectMath.Any(k => searchString.StartsWith(k, StringComparison.InvariantCultureIgnoreCase)))
        {
            keysForPerfectMath.ToList().ForEach(k =>
              searchString = Regex.Replace(searchString, Regex.Escape(k), "", RegexOptions.IgnoreCase));

            //"e.g %контакты 65" is not accepted. Space between keyword and number is prohibited
            if (!searchString.StartsWith(" ") && int.TryParse(searchString, out entityId))
            {
                return (entityId, true);
            }
        }

        // in other cases search by Id is not needed
        return (null, false);
    }
}
