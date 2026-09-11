using JoinRpg.Common.WebInfrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
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
    /// локальной валидации по JWKS. Resource-клиент для интроспекции заводится вручную через
    /// admin UI IdPortal и есть не в каждом окружении (например, в тестах его нет) — без него
    /// /mcp просто не регистрируется, а не падает при старте.
    /// </summary>
    public static IServiceCollection AddJoinMcp(this IServiceCollection services, IConfiguration configuration)
    {
        var mcpOptions = GetConfiguredOptions(configuration);
        if (mcpOptions is null)
        {
            return services;
        }

        var hostNames = configuration.GetSection("JoinRpgHostNames").Get<JoinRpgHostNamesOptions>()
            ?? throw new InvalidOperationException("JoinRpgHostNames section is required");
        var idPortalIssuer = new Uri($"https://{hostNames.IdHost}/");
        var resourceUri = new Uri($"https://{hostNames.MainHost}/mcp");

        services.AddHttpContextAccessor();
        services.AddScoped<McpAuthContext>();

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(idPortalIssuer);
                options.AddAudiences(resourceUri.ToString());
                options.UseIntrospection()
                    .SetClientId(mcpOptions.ClientId)
                    .SetClientSecret(mcpOptions.ClientSecret);
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
        if (GetConfiguredOptions(app.Configuration) is not null)
        {
            app.MapMcp("/mcp").RequireAuthorization(AuthorizationPolicy);
        }

        return app;
    }

    private static McpResourceOptions? GetConfiguredOptions(IConfiguration configuration)
    {
        var options = configuration.GetSection("Mcp").Get<McpResourceOptions>();
        return string.IsNullOrEmpty(options?.ClientId) || string.IsNullOrEmpty(options.ClientSecret)
            ? null
            : options;
    }
}
