using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace JoinRpg.Portal.Infrastructure.XApi;

/// <summary>
/// Из-за <c>JsonNumberHandling.AllowReadingFromString</c> числа описываются как
/// <c>type: ["integer", "string"]</c> с регулярным выражением. Swagger UI такие схемы не понимает
/// и не даёт вызвать метод («Required field is not provided»), поэтому оставляем только числовой тип.
/// Убрать, когда починят https://github.com/swagger-api/swagger-ui/issues/10646
/// </summary>
public sealed class StrictNumberSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        MakeStrict(schema);
        return Task.CompletedTask;
    }

    public static void MakeStrict(OpenApiSchema schema)
    {
        if (schema.Type is not { } type || !type.HasFlag(JsonSchemaType.String))
        {
            return;
        }

        var numeric = type & (JsonSchemaType.Integer | JsonSchemaType.Number);
        if (numeric == 0)
        {
            return;
        }

        schema.Type = type & ~JsonSchemaType.String;
        schema.Pattern = null;
    }
}
