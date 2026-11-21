using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// Регрессия на инцидент «/health/ready таймаутится — в логе пусто»: когда kubernetes-проба
/// обрывает соединение раньше завершения чеков (RequestAborted), отчёт не собирается и
/// фреймворковые логи чеков не пишутся. Гашение отмены должно оставлять заметную запись
/// (Warning с путём), а не растворяться на Debug.
/// </summary>
public class HealthCheckAbortedRequestLoggingTest
{
    [Fact]
    public async Task AbortedReadyRequestLogsWarning()
    {
        var logs = new CapturingLoggerProvider();
        await using var app = await StartApp(logs, checks => checks
            .AddCheck("висящий-чек", new HangingCheck(), tags: ["ready"]));

        var endpoint = FindEndpoint(app, "/health/ready");
        using var scope = app.Services.CreateScope();
        var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
        context.Request.Path = "/health/ready";
        using var aborted = new CancellationTokenSource();
        await aborted.CancelAsync();
        context.RequestAborted = aborted.Token;

        await endpoint.RequestDelegate!(context);

        logs.Entries.ShouldContain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("/health/ready"));
    }

    [Fact]
    public async Task NormalRequestDoesNotLogWarning()
    {
        var logs = new CapturingLoggerProvider();
        await using var app = await StartApp(logs, checks => checks
            .AddCheck("здоровый-чек", () => HealthCheckResult.Healthy(), tags: ["ready"]));
        using var client = app.GetTestClient();

        var response = await client.GetAsync("/health/ready");

        response.EnsureSuccessStatusCode();
        logs.Entries.ShouldNotContain(entry =>
            entry.Level == LogLevel.Warning && entry.Message.Contains("/health/ready"));
    }

    private static RouteEndpoint FindEndpoint(WebApplication app, string pattern)
        => app.Services.GetServices<EndpointDataSource>()
            .SelectMany(source => source.Endpoints)
            .OfType<RouteEndpoint>()
            .Single(endpoint => string.Equals(
                endpoint.RoutePattern.RawText, pattern, StringComparison.Ordinal));

    private static async Task<WebApplication> StartApp(
        CapturingLoggerProvider logs,
        Action<IHealthChecksBuilder> configureChecks)
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.Logging.AddProvider(logs);

        _ = builder.Services.AddJoinHealthChecks();
        configureChecks(builder.Services.AddHealthChecks());

        var app = builder.Build();
        app.MapJoinHealthChecks();
        await app.StartAsync();
        return app;
    }

    private sealed class HangingCheck : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(
            HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            return HealthCheckResult.Healthy();
        }
    }

    private sealed record LogEntry(LogLevel Level, string Message, Exception? Exception);

    private sealed class CapturingLoggerProvider : ILoggerProvider
    {
        public List<LogEntry> Entries { get; } = [];

        public ILogger CreateLogger(string categoryName) => new CapturingLogger(Entries);

        public void Dispose() { }

        private sealed class CapturingLogger(List<LogEntry> entries) : ILogger
        {
            IDisposable? ILogger.BeginScope<TState>(TState state) => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
                => entries.Add(new LogEntry(logLevel, formatter(state, exception), exception));
        }
    }
}
