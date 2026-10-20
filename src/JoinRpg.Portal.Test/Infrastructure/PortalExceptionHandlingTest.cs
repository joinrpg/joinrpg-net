using System.Collections.Concurrent;
using System.Net;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Portal.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Logging;

namespace JoinRpg.Portal.Test.Infrastructure;

/// <summary>
/// Ссылка на несуществующий проект — штатный отказ: 404 и ни одной записи уровня Error (#5709).
/// Настоящий сбой по-прежнему даёт 500 и попадает в лог как ошибка.
/// </summary>
public class PortalExceptionHandlingTest
{
    [Fact]
    public async Task ProjectNotFound_Returns404WithoutErrorLog()
    {
        var logs = new RecordingLoggerProvider();
        await using var app = await StartApp(logs);

        var response = await app.GetTestClient().GetAsync("/throw-project-not-found");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        (await response.Content.ReadAsStringAsync()).ShouldBe("error page: 404");
        logs.Levels.ShouldNotContain(LogLevel.Error);
    }

    [Fact]
    public async Task OtherEntityNotFound_StaysServerError()
    {
        var logs = new RecordingLoggerProvider();
        await using var app = await StartApp(logs);

        var response = await app.GetTestClient().GetAsync("/throw-claim-not-found");

        response.StatusCode.ShouldBe(HttpStatusCode.InternalServerError);
        logs.Levels.ShouldContain(LogLevel.Error);
    }

    private static async Task<WebApplication> StartApp(RecordingLoggerProvider logs)
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.Logging.ClearProviders().AddProvider(logs);

        var app = builder.Build();
        _ = app.UseExceptionHandler(PortalExceptionHandling.CreateOptions());
        app.Run(context => context.Request.Path.Value switch
        {
            PortalExceptionHandling.ErrorPagePath => context.Response.WriteAsync($"error page: {context.Response.StatusCode}"),
            "/throw-project-not-found" => throw new ProjectNotFoundException(new ProjectIdentification(2024)),
            _ => throw new JoinRpgEntityNotFoundException(1, "claim"),
        });
        await app.StartAsync();
        return app;
    }

    private sealed class RecordingLoggerProvider : ILoggerProvider, ILogger
    {
        private readonly ConcurrentQueue<LogLevel> levels = new();

        public IReadOnlyCollection<LogLevel> Levels => levels;

        public ILogger CreateLogger(string categoryName) => this;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => levels.Enqueue(logLevel);

        public void Dispose() { }
    }
}
