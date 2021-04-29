using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.Helpers;
using Microsoft.AspNetCore.Http;

namespace Joinrpg.AspNetCore.Helpers
{
    public static class FormCollectionHelpers
    {
        private static Dictionary<string, string?> ToDictionary(this IFormCollection collection) => collection.Keys.ToDictionary(key => key, key => (string?)collection[key].First());

        public static IReadOnlyDictionary<int, string?> GetDynamicValuesFromPost(this HttpRequest request, string prefix)
        {
            var post = request.Form.ToDictionary();
            return post.Keys.UnprefixNumbers(prefix)
                .ToDictionary(fieldClientId => fieldClientId,
                    fieldClientId => post[prefix + fieldClientId]);
        }
    }
}
