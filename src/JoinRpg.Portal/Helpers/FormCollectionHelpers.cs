using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Helpers;

namespace Joinrpg.AspNetCore.Helpers;

public static class FormCollectionHelpers
{
    private static Dictionary<string, string?> ToDictionary(this IFormCollection collection) => collection.Keys.ToDictionary(key => key, key => TransformToString(collection, key));
    private static string? TransformToString(IFormCollection collection, string key)
    {
        Microsoft.Extensions.Primitives.StringValues value = collection[key];
        return value.ToString();
    }

    /// <summary>
    /// Поля, пришедшие из формы, — сразу слоем. Граница, на которой нетипизированный
    /// «id поля → строка» превращается в доменный объект и проверяется по метаданным проекта.
    /// </summary>
    public static FieldLayerContainer GetFieldsToSetFromPost(this HttpRequest request, ProjectInfo projectInfo, string prefix)
        => FieldLayerContainer.FromFieldValues(projectInfo, request.GetDynamicValuesFromPost(prefix));

    public static Dictionary<int, string?> GetDynamicValuesFromPost(this HttpRequest request, string prefix)
    {
        var post = request.Form.ToDictionary();
        return post.Keys.UnprefixNumbers(prefix)
            .ToDictionary(fieldClientId => fieldClientId,
                fieldClientId => post[prefix + fieldClientId]);
    }
}
