using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenIddict.Validation.AspNetCore;

namespace JoinRpg.Mcp;

public static class McpRegistration
{
    /// <summary>Именованная политика авторизации для /mcp — не трогает DefaultPolicy сайта.</summary>
    public const string AuthorizationPolicy = "Mcp";

    /// <summary>
    /// Portal как resource server (ADR012 §5): интроспекция токенов через IdPortal, никакой
    /// локальной валидации по JWKS. <paramref name="idPortalIssuer"/> и <paramref name="resourceUri"/> —
    /// полные URL с завершающим https://.
    /// </summary>
    public static IServiceCollection AddJoinMcp(
        this IServiceCollection services,
        Uri idPortalIssuer,
        Uri resourceUri,
        string resourceClientId,
        string resourceClientSecret)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<McpAuthContext>();

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(idPortalIssuer);
                options.AddAudiences(resourceUri.ToString());
                options.UseIntrospection()
                    .SetClientId(resourceClientId)
                    .SetClientSecret(resourceClientSecret);
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        // Отдельная именованная схема только для WWW-Authenticate/Protected Resource Metadata
        // (RFC 9728) — сам токен по-прежнему проверяет OpenIddictValidationAspNetCoreDefaults.
        services.AddAuthentication()
            .AddMcp(options =>
            {
                options.ResourceMetadata = new()
                {
                    Resource = resourceUri.ToString(),
                    AuthorizationServers = { idPortalIssuer.ToString() },
                };
            });

        services.AddAuthorization(o => o.AddPolicy(AuthorizationPolicy, policy => policy
            .AddAuthenticationSchemes(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                McpAuthenticationDefaults.AuthenticationScheme)
            .RequireAuthenticatedUser()));

        services.AddMcpServer(options => options.ServerInstructions = McpInstructions.Text)
            .WithHttpTransport()
            .WithTools<CharacterMcpTools>()
            .WithTools<ProjectMcpTools>();

        return services;
    }

    public static WebApplication MapJoinMcp(this WebApplication app)
    {
        app.MapMcp("/mcp").RequireAuthorization(AuthorizationPolicy);
        return app;
    }
}
