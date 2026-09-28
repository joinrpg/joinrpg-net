using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure;

public static class HealthCheckExtensions
{
    /// <summary>
    /// Таймаут, который проставляется чекам, зарегистрированным без явного timeout:.
    /// </summary>
    public static readonly TimeSpan DefaultHealthCheckTimeout = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Регистрирует health-чеки и гарантирует, что у каждого из них есть таймаут.
    /// </summary>
    /// <remarks>
    /// Чеки заводятся в разных библиотеках (БД, S3, Telegram, MCP), и чек без timeout: висит
    /// столько, сколько сочтёт нужным внешний сервис — вместе с ним висит весь /health
    /// (см. issue #4994). Проставляем дефолт централизованно: явно заданные таймауты не трогаем.
    /// </remarks>
    public static IServiceCollection AddJoinHealthChecks(this IServiceCollection services)
    {
        _ = services.AddHealthChecks();

        _ = services.PostConfigure<HealthCheckServiceOptions>(options =>
        {
            foreach (var registration in options.Registrations)
            {
                if (registration.Timeout == Timeout.InfiniteTimeSpan)
                {
                    registration.Timeout = DefaultHealthCheckTimeout;
                }
            }
        });

        return services;
    }

    public static void MapJoinHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        _ = endpoints.MapJoinHealthCheck("/health",
            new HealthCheckOptions { ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse });

        _ = endpoints.MapJoinHealthCheck("/health/ready", new HealthCheckOptions()
        {
            Predicate = (check) => check.Tags.Contains("ready"),
        });

        _ = endpoints.MapJoinHealthCheck("/health/live", new HealthCheckOptions()
        {
            Predicate = (_) => false
        });
    }

    private static IEndpointConventionBuilder MapJoinHealthCheck(
        this IEndpointRouteBuilder endpoints,
        string pattern,
        HealthCheckOptions options)
    {
        var builder = endpoints.MapHealthChecks(pattern, options)
            .WithMetadata(new AllowAnonymousAttribute());
        builder.Add(IgnoreAbortedRequest);
        return builder;
    }

    /// <summary>
    /// Если клиент (или kubernetes-проба) закрыл соединение, движок health-чеков пробрасывает
    /// OperationCanceledException наружу, и запрос попадает в логи как 500 — хотя писать ответ
    /// всё равно уже некому. Гасим такую отмену, чтобы 500 не путал разбор инцидентов.
    /// </summary>
    private static void IgnoreAbortedRequest(EndpointBuilder builder)
    {
        var next = builder.RequestDelegate;
        if (next is null)
        {
            return;
        }

        builder.RequestDelegate = async context =>
        {
            try
            {
                await next(context);
            }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
            {
                context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger(typeof(HealthCheckExtensions))
                    .LogDebug("Запрос health-чека отменён клиентом, ответ не отправлен");
            }
        };
    }
}
