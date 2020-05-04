using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace JoinRpg.Services.Impl.Search
{
    internal static class SearchKeywordsResolver
    {
        public static int? TryGetId(
          string searchString,
          string[] keysForPerfectMath,
          out bool whenFoundItIsPerfectMatch)
        {
            int entityId;
            // bare number in search string requires, among other, search by id. No perfect match.
            if (Int32.TryParse(searchString.Trim(), out entityId))
            {
                whenFoundItIsPerfectMatch = false;
                return entityId;
            }

            //"%контакты4196", "персонаж4196", etc provide a perfect match
            if (keysForPerfectMath.Any(k => searchString.StartsWith(k, StringComparison.InvariantCultureIgnoreCase)))
            {
                keysForPerfectMath.ToList().ForEach(k =>
                  searchString = Regex.Replace(searchString, Regex.Escape(k), "", RegexOptions.IgnoreCase));

                //"e.g %контакты 65" is not accepted. Space between keyword and number is prohibited
                if (!searchString.StartsWith(" ") && Int32.TryParse(searchString, out entityId))
                {
                    whenFoundItIsPerfectMatch = true;
                    return entityId;
                }
            }

            // in other cases search by Id is not needed
            whenFoundItIsPerfectMatch = false;
            return null;
        }
    }
}
