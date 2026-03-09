using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace JoinRpg.Portal.Infrastructure.XApi;

internal static class Swagger
{
    internal static IServiceCollection AddJoinXApiSwagger(this IServiceCollection services)
    {
        return services.AddOpenApi("v1", options =>
        {
            _ = options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo { Title = "My API", Version = "v1" };
                return Task.CompletedTask;
            })
            .AddDocumentTransformer(new JwtSecuritySchemeTransformer())
            .AddDocumentTransformer(new XGameApiPathFilter());
        });
    }
    internal static void ConfigureUI(SwaggerUIOptions c)
    {
        c.SwaggerEndpoint("/openapi/v1.json", "My API V1");
        c.ConfigObject.DeepLinking = true;
    }

    private sealed class JwtSecuritySchemeTransformer : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "JWT Authentication",
                Description = "Enter JWT Bearer token **_only_**",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes[JwtBearerDefaults.AuthenticationScheme] = securityScheme;

            var schemeRef = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme, document);
            document.Security ??= [];
            document.Security.Add(new OpenApiSecurityRequirement { { schemeRef, [] } });

            return Task.CompletedTask;
        }
    }

    private sealed class XGameApiPathFilter : IOpenApiDocumentTransformer
    {
        public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
        {
            if (document.Paths is null)
            {
                return Task.CompletedTask;
            }

            foreach (var item in document.Paths.ToList())
            {
                if (!item.Key.ToLower().IsExternalApiPath())
                {
                    _ = document.Paths.Remove(item.Key);
                }
            }

            return Task.CompletedTask;
        }
    }
}
