using System.Reflection;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace JoinRpg.Portal.Infrastructure;

internal class Swagger
{
    internal static void ConfigureSwagger(SwaggerGenOptions c)
    {
        var securityScheme = new OpenApiSecurityScheme
        {
            Name = "JWT Authentication",
            Description = "Enter JWT Bearer token **_only_**",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.Http,
            Scheme = "bearer", // must be lower case
            BearerFormat = "JWT",
            Reference = new OpenApiReference
            {
                Id = JwtBearerDefaults.AuthenticationScheme,
                Type = ReferenceType.SecurityScheme
            }
        };
        c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {securityScheme, new string[] { }}
            });

        c.SwaggerDoc("v1", new OpenApiInfo()
        {
            Title = "My API",
            Version = "v1"
        });
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml"));
        c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, $"JoinRpg.Web.XGameApi.Contract.xml"));

        c.DocumentFilter<SwaggerXGameApiFilter>();
    }

    internal static void Configure(SwaggerOptions options) { }

    internal static void ConfigureUI(SwaggerUIOptions c)
    {
        c.SwaggerEndpoint("v1/swagger.json", "My API V1");
        c.ConfigObject.DeepLinking = true;
    }

    internal static Task RedirectToSwagger(HttpContext ctx)
    {
        ctx.Response.Redirect("swagger/");
        return Task.CompletedTask;
    }

    private class SwaggerXGameApiFilter : IDocumentFilter
    {
        public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
        {
            foreach (var item in swaggerDoc.Paths.ToList())
            {
                var key = item.Key.ToLower();
                if (!key.IsApiPath())
                {
                    _ = swaggerDoc.Paths.Remove(item.Key);
                }
            }

        }


    }
}
