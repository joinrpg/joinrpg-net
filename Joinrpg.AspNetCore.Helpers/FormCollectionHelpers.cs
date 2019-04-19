using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace Joinrpg.AspNetCore.Helpers
{
    public static class FormCollectionHelpers
    {
        public static Dictionary<string, string> ToDictionary(this IFormCollection collection)
        {
            return collection.Keys.ToDictionary(key => key, key => collection[key].First());
        }
    }
}
