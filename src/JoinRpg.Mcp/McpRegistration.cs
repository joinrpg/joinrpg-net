using JoinRpg.Common.WebInfrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.AspNetCore.Authentication;
using OpenIddict.Validation.AspNetCore;

namespace JoinRpg.Mcp;

public static class McpRegistration
{
    /// <summary>Именованная политика авторизации для /mcp — не трогает DefaultPolicy сайта.</summary>
    public const string AuthorizationPolicy = "Mcp";

    /// <summary>Имя health check, по которому видно, включён ли MCP в этом окружении.</summary>
    public const string McpHealthCheckName = "mcp";

    /// <summary>
    /// Portal как resource server (ADR012 §5): интроспекция токенов через IdPortal, никакой
    /// локальной валидации по JWKS. Resource-клиент для интроспекции заводится вручную через
    /// admin UI IdPortal и есть не в каждом окружении (например, в тестах его нет) — без него
    /// /mcp просто не регистрируется, а не падает при старте.
    /// </summary>
    public static IServiceCollection AddJoinMcp(
        this IServiceCollection services,
        McpResourceOptions? mcpOptions,
        JoinRpgHostNamesOptions hostNames)
    {
        var enabled = !string.IsNullOrEmpty(mcpOptions?.ClientId) && !string.IsNullOrEmpty(mcpOptions.ClientSecret);
        var idPortalIssuer = new Uri($"https://{hostNames.IdHost}/");
        var resourceUri = new Uri($"https://{hostNames.MainHost}/mcp");

        // IdPortal пускает в интроспекцию только сторону, указанную в аудитории токена, а
        // аудитория у нас — идентификатор ресурса. Значит client_id, которым мы представляемся
        // при интроспекции, обязан быть равен ему же. Иначе IdPortal отвечает
        // 200 {"active": false} на совершенно нормальный токен, и наружу это выходит как 401
        // на /mcp без единого намёка на причину — так и случилось на dev.
        var clientIdMatchesResource =
            string.Equals(mcpOptions?.ClientId, resourceUri.ToString(), StringComparison.Ordinal);

        // Регистрируется всегда, в том числе когда MCP выключен: иначе «выключен» и «сломан»
        // снаружи неотличимы — оба дают 404 на /mcp.
        //
        // Выключенный MCP — Degraded, а не Healthy: это не нормальное состояние, а
        // недонастроенное окружение. При этом Degraded не отдаёт 503 (по умолчанию 200) и
        // тегом ready не помечен, так что ни liveness, ни readiness не ломает — инстанс
        // продолжает обслуживать сайт, у которого просто нет MCP.
        services.AddHealthChecks().AddCheck(
            McpHealthCheckName,
            () => !enabled
                ? HealthCheckResult.Degraded("MCP выключен: не заданы Mcp:ClientId/Mcp:ClientSecret")
                : clientIdMatchesResource
                    ? HealthCheckResult.Healthy("MCP включён, /mcp зарегистрирован")
                    : HealthCheckResult.Degraded(
                        $"MCP включён, но Mcp:ClientId ('{mcpOptions!.ClientId}') не равен ресурсу "
                        + $"('{resourceUri}'). IdPortal пускает в интроспекцию только сторону из "
                        + "аудитории токена, поэтому все токены будут отвергаться как неактивные, "
                        + "а /mcp отвечать 401."));

        if (!enabled)
        {
            return services;
        }

        services.AddHttpContextAccessor();
        services.AddScoped<McpAuthContext>();

        // Маркер «MCP зарегистрирован» — MapJoinMcp смотрит на него, а не выводит то же
        // решение из конфига второй раз.
        services.AddSingleton<McpEnabledMarker>();

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

        // Схема MCP отвечает за WWW-Authenticate и Protected Resource Metadata (RFC 9728), а
        // саму проверку токена переадресует в OpenIddict. Раньше в политике стояли обе схемы,
        // и на 401 приходило два заголовка WWW-Authenticate: голый Bearer от OpenIddict и
        // Bearer resource_metadata=... от MCP. Клиент, читающий только первый, не находил
        // указателя на метаданные. Forward оставляет ровно один challenge — от MCP.
        services.AddAuthentication()
            // displayName: null — схема MCP не интерактивный провайдер логина. С непустым
            // DisplayName (по умолчанию он есть) она попадала в выдачу
            // SignInManager.GetExternalAuthenticationSchemesAsync, и на /Account/Login рядом с
            // Google и ВК рисовалась кнопка «Войти через MCP Authentication», которая ничего не логинит.
            // null! — параметр в библиотеке не помечен как nullable, хотя AddScheme его таким принимает.
            .AddMcp(McpAuthenticationDefaults.AuthenticationScheme, displayName: null!, options =>
            {
                options.ForwardAuthenticate = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
                options.ResourceMetadata = new()
                {
                    Resource = resourceUri.ToString(),
                    AuthorizationServers = { idPortalIssuer.ToString() },
                    // Без этого список уезжает пустым, и клиент не знает, какие scope просить
                    // (RFC 9728 §2: ровно отсюда он их и узнаёт). Публикуем только те, что
                    // сервер умеет, — см. McpScopes.Advertised.
                    ScopesSupported = [.. McpScopes.Advertised],
                };
            });

        // Только схема MCP: аутентификацию она форвардит в OpenIddict (см. ForwardAuthenticate
        // выше), а challenge выдаёт сама — одним заголовком с resource_metadata.
        services.AddAuthorization(o => o.AddPolicy(AuthorizationPolicy, policy => policy
            .AddAuthenticationSchemes(
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
        if (app.Services.GetService<McpEnabledMarker>() is not null)
        {
            app.MapMcp("/mcp").RequireAuthorization(AuthorizationPolicy);
            app.Logger.LogInformation("MCP включён: эндпоинт /mcp зарегистрирован (ADR012)");
        }
        else
        {
            // Без этой строчки выключенный MCP выглядит снаружи ровно как сломанный — 404 и
            // тишина в логах. Именно так он и уехал на dev незамеченным.
            app.Logger.LogWarning(
                "MCP выключен: не заданы Mcp:ClientId/Mcp:ClientSecret, эндпоинт /mcp не зарегистрирован. "
                + "Resource-клиента заводят через админку IdPortal («Создать resource server»).");
        }

        return app;
    }

    private sealed class McpEnabledMarker;
}
