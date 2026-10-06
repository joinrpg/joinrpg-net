using System.Diagnostics;
using System.Net;
using System.Text;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Telegram.Bot;

namespace JoinRpg.Common.Telegram.Test;

/// <summary>
/// Регрессия на #5302: чек не ловил исключения, и любая ошибка Telegram API долетала до движка
/// health-чеков как необработанное исключение вместо диагноза.
/// </summary>
public class HealthCheckTelegramTest
{
    [Fact]
    public async Task DisabledTelegramIsDegraded()
    {
        var result = await RunCheck(new TelegramLoginOptions { BotName = "", BotId = null, BotSecret = "" }, new NoServiceProvider());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task MissingSecretIsDegraded()
    {
        var result = await RunCheck(new TelegramLoginOptions { BotName = "bot", BotId = 123456, BotSecret = "" }, new NoServiceProvider());

        result.Status.ShouldBe(HealthStatus.Degraded);
    }

    [Fact]
    public async Task WorkingTelegramIsHealthy()
    {
        var result = await RunCheck(
            ConfiguredOptions,
            new RespondingHttpMessageHandler(
                HttpStatusCode.OK,
                """{"ok":true,"result":{"id":123456,"is_bot":true,"first_name":"Bot","username":"joinrpg_bot"}}"""));

        result.Status.ShouldBe(HealthStatus.Healthy);
        result.Description.ShouldNotBeNull().ShouldContain("joinrpg_bot");
    }

    [Fact]
    public async Task ApiErrorIsUnhealthy()
    {
        var result = await RunCheck(
            ConfiguredOptions,
            new RespondingHttpMessageHandler(
                HttpStatusCode.Unauthorized,
                """{"ok":false,"error_code":401,"description":"Unauthorized"}"""));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        _ = result.Exception.ShouldNotBeNull();
        result.Description.ShouldNotBeNull().ShouldContain("401");
    }

    [Fact]
    public async Task NetworkErrorIsUnhealthy()
    {
        var result = await RunCheck(
            ConfiguredOptions,
            new ThrowingHttpMessageHandler(new HttpRequestException("Connection refused")));

        result.Status.ShouldBe(HealthStatus.Unhealthy);
        _ = result.Exception.ShouldNotBeNull();
    }

    [Fact]
    public async Task HangingTelegramIsCancelledByToken()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));

        _ = await Should.ThrowAsync<OperationCanceledException>(
            () => RunCheck(ConfiguredOptions, new HangingHttpMessageHandler(), cts.Token));
    }

    private static TelegramLoginOptions ConfiguredOptions => new() { BotName = "joinrpg_bot", BotId = 123456, BotSecret = "ABC-DEF1234ghIkl-zyx57W2v1u123ew11" };

    private static Task<HealthCheckResult> RunCheck(
        TelegramLoginOptions options,
        HttpMessageHandler handler,
        CancellationToken cancellationToken = default)
    {
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://api.telegram.org") };
        var botClient = new TelegramBotClient($"{options.BotId}:{options.BotSecret}", httpClient);
        var service = new TelegramNotificationServiceImpl(botClient, NullLogger<TelegramNotificationServiceImpl>.Instance);
        return RunCheck(options, new SingleServiceProvider(service), cancellationToken);
    }

    private static Task<HealthCheckResult> RunCheck(
        TelegramLoginOptions options,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        var check = new HealthCheckTelegram(
            Options.Create(options),
            serviceProvider,
            NullLogger<HealthCheckTelegram>.Instance);
        return check.CheckHealthAsync(new HealthCheckContext(), cancellationToken);
    }

    private sealed class SingleServiceProvider(object service) : IServiceProvider
    {
        public object? GetService(Type serviceType) => serviceType.IsInstanceOfType(service) ? service : null;
    }

    private sealed class NoServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType)
            => throw new InvalidOperationException("Ненастроенный Telegram не должен создавать клиент");
    }

    private sealed class HangingHttpMessageHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(Timeout.Infinite, CancellationToken.None).WaitAsync(cancellationToken);
            throw new UnreachableException();
        }
    }

    private sealed class ThrowingHttpMessageHandler(Exception exception) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromException<HttpResponseMessage>(exception);
    }

    private sealed class RespondingHttpMessageHandler(HttpStatusCode statusCode, string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
    }
}
