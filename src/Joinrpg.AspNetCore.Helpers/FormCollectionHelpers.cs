using JoinRpg.Helpers;
using Microsoft.AspNetCore.Http;

namespace Joinrpg.AspNetCore.Helpers
{
    public static class FormCollectionHelpers
    {
        private static Dictionary<string, string?> ToDictionary(this IFormCollection collection) => collection.Keys.ToDictionary(key => key, key => TransformToString(collection, key));
        private static string? TransformToString(IFormCollection collection, string key)
        {
            Microsoft.Extensions.Primitives.StringValues value = collection[key];
            return value.ToString();
        }

        public static IReadOnlyDictionary<int, string?> GetDynamicValuesFromPost(this HttpRequest request, string prefix)
        {
            var post = request.Form.ToDictionary();
            return post.Keys.UnprefixNumbers(prefix)
                .ToDictionary(fieldClientId => fieldClientId,
                    fieldClientId => post[prefix + fieldClientId]);
        }
    }
}
