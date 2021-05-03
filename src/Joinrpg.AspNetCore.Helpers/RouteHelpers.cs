using System;
using Microsoft.AspNetCore.Routing;

namespace Joinrpg.AspNetCore.Helpers
{
    public static class RouteHelpers
    {
        [Obsolete]
        public static string? GetRequiredString(this RouteData routeData, string keyName)
        {
            if (!routeData.Values.TryGetValue(keyName, out var value))
            {
                throw new InvalidOperationException($"Could not find key with name '{keyName}'");
            }

            return value?.ToString();
        }
    }
}
