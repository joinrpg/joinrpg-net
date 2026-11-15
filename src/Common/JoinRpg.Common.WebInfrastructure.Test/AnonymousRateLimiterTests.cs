using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Глобальный лимитер одновременных анонимных запросов (#6232): сверх лимита анонимный запрос
/// получает 429 до обращения к БД, авторизованные не лимитируются, health-endpoint'ы исключены.
/// </summary>
public class AnonymousRateLimiterTests
{
    [Fact]
    public void AuthenticatedUser_GetsNoLimiterPartition()
    {
        var context = new DefaultHttpContext();
        context.User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], authenticationType: "test"));

        var partition = AnonymousRateLimiterExtensions.GetRateLimitPartition(context, permitLimit: 1);

        partition.PartitionKey.ShouldBe("authenticated");
        var limiter = partition.Factory("authenticated");
        // No-limiter: одновременные lease'ы не ограничены — хоть все сто подряд
        var leases = Enumerable.Range(0, 100).Select(_ => limiter.AttemptAcquire()).ToList();
        leases.ShouldAllBe(l => l.IsAcquired);
    }

    [Fact]
    public void AnonymousUser_GetsConcurrencyLimiterWithConfiguredPermitLimit()
    {
        var context = new DefaultHttpContext(); // анонимный: User.Identity == null
        var partition = AnonymousRateLimiterExtensions.GetRateLimitPartition(context, permitLimit: 2);

        partition.PartitionKey.ShouldBe("anonymous");
        var limiter = partition.Factory("anonymous").ShouldBeOfType<ConcurrencyLimiter>();
        limiter.AttemptAcquire().IsAcquired.ShouldBeTrue();
        limiter.AttemptAcquire().IsAcquired.ShouldBeTrue();
        // Третий одновременный запрос — сразу отказ, очереди нет
        limiter.AttemptAcquire().IsAcquired.ShouldBeFalse();
    }

    [Fact]
    public async Task AnonymousRequestsOverLimit_Get429_WhileAuthenticatedAndHealthCheckAreNotLimited()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        using var server = CreateServer(
            permitLimit: 1,
            block: async context =>
            {
                started.TrySetResult();
                await gate.Task;
                await context.Response.WriteAsync("ok");
            });
        var client = server.CreateClient();

        var blocked = client.GetAsync("/block");
        await started.Task.WaitAsync(TimeSpan.FromSeconds(30));

        // Первый запрос держит lease — второй анонимный получает 429. Дальше StatusCodePages
        // перезапускает pipeline на /error/429: endpoint с DisableRateLimiting (как ErrorPageModel
        // в Portal) отрабатывает при исчерпанном бакете — клиент получает тело страницы, а не пустой 429.
        var rejected = await client.GetAsync("/block");
        rejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        (await rejected.Content.ReadAsStringAsync()).ShouldBe("error page: 429");

        // Отказ лимитера по API-пути (/webapi островов) — ProblemDetails, а не HTML: WASM-острова
        // не смогли бы распарсить HTML-страницу и висли бы в «Идёт загрузка...»
        var apiRejected = await client.GetAsync("/webapi/test");
        apiRejected.StatusCode.ShouldBe(HttpStatusCode.TooManyRequests);
        apiRejected.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await apiRejected.Content.ReadAsStringAsync())
            .ShouldContain("Слишком много запросов — подождите минуту и обновите страницу");

        // Health-endpoint'ы с DisableRateLimiting доходят до чеков, когда бакет анонимов исчерпан
        var health = await client.GetAsync("/health/live");
        health.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Запрос, авторизованный через политику (principal выставлен до лимитера, как у x-api/MCP
        // в AuthorizationMiddleware), лимитером не трогается
        var authenticated = await client.GetAsync("/auth/page");
        authenticated.StatusCode.ShouldBe(HttpStatusCode.OK);

        gate.TrySetResult();
        (await blocked).StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData("/health")]
    [InlineData("/health/ready")]
    [InlineData("/health/live")]
    public void HealthEndpoints_AreExcludedFromRateLimiting(string route)
    {
        using var server = new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                _ = services.AddRouting();
                _ = services.AddHealthChecks();
            })
            .Configure(app =>
            {
                _ = app.UseRouting();
                _ = app.UseEndpoints(endpoints => endpoints.MapJoinHealthChecks());
            }));

        var endpoint = server.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Single(ep => ep.RoutePattern.RawText == route);

        endpoint.Metadata.GetMetadata<DisableRateLimitingAttribute>().ShouldNotBeNull();
    }

    private static TestServer CreateServer(int permitLimit, RequestDelegate block)
    {
        var authenticatedUser = new ClaimsPrincipal(
            new ClaimsIdentity([new Claim(ClaimTypes.Name, "user")], authenticationType: "test"));
        var builder = new WebHostBuilder()
            .ConfigureServices(services =>
            {
                _ = services.AddRouting();
                _ = services.AddAuthorization();
                _ = services.AddHealthChecks();
                _ = services.AddProblemDetails();
                _ = services.AddRateLimiter(options => AnonymousRateLimiterExtensions.ConfigureAnonymousRateLimiter(options, permitLimit));
            })
            .Configure(app =>
            {
                // Симуляция аутентификации через политику (x-api JWT, MCP): principal появляется
                // в AuthorizationMiddleware до лимитера.
                _ = app.Use((context, next) =>
                {
                    if (context.Request.Path.StartsWithSegments("/auth"))
                    {
                        context.User = authenticatedUser;
                    }
                    return next();
                });
                // Как в проде: отказ с пустым телом перезапускается на страницу ошибки.
                _ = app.UseStatusCodePagesWithReExecute("/error/{0}");
                // Копия ветви ProblemDetails из Startup Portal'а: отказы по API-путям (/webapi
                // и внешнее) отдаём ProblemDetails, а не HTML-страницей ошибки. В проде путь
                // проверяет IsApiPath(), заголовок — WebApiClientErrorTitles (типы Portal'а).
                _ = app.Use(async (context, next) =>
                {
                    var isApi = context.Request.Path.StartsWithSegments("/webapi");
                    if (isApi && context.Features.Get<IStatusCodePagesFeature>() is { } statusCodePages)
                    {
                        statusCodePages.Enabled = false;
                    }

                    await next();

                    if (isApi
                        && context.Response.StatusCode is >= 400 and < 600
                        && !context.Response.HasStarted
                        && (!context.Response.Body.CanSeek || context.Response.Body.Length == 0))
                    {
                        var status = context.Response.StatusCode;
                        await context.RequestServices.GetRequiredService<IProblemDetailsService>()
                            .TryWriteAsync(new ProblemDetailsContext
                            {
                                HttpContext = context,
                                ProblemDetails = new ProblemDetails
                                {
                                    Status = status,
                                    Title = "Слишком много запросов — подождите минуту и обновите страницу",
                                },
                            });
                    }
                });
                _ = app.UseRouting();
                _ = app.UseAuthorization();
                // Как в проде: после AuthorizationMiddleware, до выполнения endpoint'а
                _ = app.UseJoinAnonymousRateLimiter();
                _ = app.UseEndpoints(endpoints =>
                {
                    _ = endpoints.MapGet("/block", block);
                    _ = endpoints.MapGet("/auth/page", context => context.Response.WriteAsync("ok"));
                    _ = endpoints.MapGet("/webapi/test", context => context.Response.WriteAsync("ok"));
                    // Как [DisableRateLimiting] на ErrorPageModel: страница ошибки доступна,
                    // когда бакет анонимов исчерпан.
                    _ = endpoints.MapGet("/error/{code}", (string code) => Results.Text($"error page: {code}"))
                        .DisableRateLimiting();
                    endpoints.MapJoinHealthChecks();
                });
            });
        return new TestServer(builder);
    }
}
