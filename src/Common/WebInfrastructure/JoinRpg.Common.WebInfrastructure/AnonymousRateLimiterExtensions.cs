using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Глобальный лимитер одновременных анонимных запросов: ботнет держит сотни параллельных
/// запросов и кладёт SQL Server. Авторизованные пользователи не лимитируются, счётчик per-pod
/// (каждая реплика — свой процесс и свой пул соединений), сверх лимита — быстрый 429 (#6232).
/// </summary>
public static class AnonymousRateLimiterExtensions
{
    /// <summary>
    /// Регистрирует глобальный concurrency-лимитер анонимных запросов: сверх лимита анонимный
    /// запрос получает быстрый 429 до обращения к БД, авторизованные не лимитируются.
    /// </summary>
    /// <remarks>
    /// Партиция — один общий бакет «anonymous» на реплику: per-IP лимиты не останавливают
    /// распределённый ботнет с одним запросом с каждого IP. Лимит — <c>AnonymousRateLimit:PermitLimit</c>
    /// (по умолчанию 10): легитимный анонимный трафик — 1–3 одновременных запроса на сайт.
    /// </remarks>
    public static IServiceCollection AddJoinAnonymousRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        // Значение <= 0 из конфига не должно молча резать всех анонимов до нуля.
        var permitLimit = Math.Max(1, configuration.GetValue<int?>("AnonymousRateLimit:PermitLimit") ?? 10);
        _ = services.AddRateLimiter(options => ConfigureAnonymousRateLimiter(options, permitLimit));
        return services;
    }

    /// <summary>
    /// Включает глобальный лимитер анонимных запросов (см. <see cref="AddJoinAnonymousRateLimiter"/>).
    /// </summary>
    /// <remarks>
    /// СТАВИТЬСЯ ПОСЛЕ <c>UseAuthentication()</c>/<c>UseAuthorization()</c>: запросы, аутентифицируемые
    /// через политики (x-api JWT, MCP через OpenIddict validation), получают principal только в
    /// AuthorizationMiddleware — до него для лимитера они выглядели бы анонимными и получали бы 429 (#6232).
    /// </remarks>
    public static IApplicationBuilder UseJoinAnonymousRateLimiter(this IApplicationBuilder builder) => builder.UseRateLimiter();

    internal static void ConfigureAnonymousRateLimiter(RateLimiterOptions options, int permitLimit)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context => GetRateLimitPartition(context, permitLimit));
        options.OnRejected = (context, cancellationToken) =>
        {
            context.HttpContext.RequestServices
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger(typeof(AnonymousRateLimiterExtensions))
                .LogWarning(
                    "Анонимный запрос {Method} {Path} отклонён лимитером: превышен лимит одновременных запросов",
                    context.HttpContext.Request.Method,
                    context.HttpContext.Request.Path);
            return ValueTask.CompletedTask;
        };
    }

    /// <summary>
    /// Один общий бакет «anonymous» на реплику: per-IP лимиты не останавливают распределённый ботнет
    /// с одним запросом с каждого IP (#6232).
    /// </summary>
    internal static RateLimitPartition<string> GetRateLimitPartition(HttpContext context, int permitLimit)
        => context.User.Identity?.IsAuthenticated == true
            ? RateLimitPartition.GetNoLimiter("authenticated")
            : RateLimitPartition.GetConcurrencyLimiter("anonymous", _ => new ConcurrencyLimiterOptions
            {
                PermitLimit = permitLimit,
                // Сверх лимита — сразу 429, до SQL запрос не должен дождаться своей очереди
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0,
            });
}
