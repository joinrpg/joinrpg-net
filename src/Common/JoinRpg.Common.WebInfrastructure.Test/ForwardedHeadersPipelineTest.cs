using System.Diagnostics.Metrics;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace JoinRpg.Common.WebInfrastructure;

/// <summary>
/// ForwardedHeaders и метрика запросов из локальной сети встраиваются в начало конвейера без участия приложения.
/// </summary>
public class ForwardedHeadersPipelineTest
{
    private const string RemoteIpHeader = "X-Test-Remote-Ip";

    [Fact]
    public async Task ForwardedForIsAppliedBeforeAppMiddleware()
    {
        await using var app = await StartApp();

        var context = await Send(app, "/", forwardedFor: "8.8.8.8");

        context.Response.Headers[RemoteIpHeader].ToString().ShouldBe("8.8.8.8");
    }

    [Fact]
    public async Task PublicAddressIsNotCounted()
    {
        await using var app = await StartApp();
        using var metrics = new MetricsRecorder(app);

        _ = await Send(app, "/", remoteIp: "8.8.8.8");
        _ = await Send(app, "/", remoteIp: "10.0.0.5", forwardedFor: "8.8.8.8");

        metrics.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task DirectRequestFromPrivateNetworkIsCountedAsNotForwarded()
    {
        await using var app = await StartApp();
        using var metrics = new MetricsRecorder(app);

        _ = await Send(app, "/", remoteIp: "10.0.0.5");

        metrics.Measurements.ShouldBe(["false"]);
    }

    [Fact]
    public async Task ForwardedPrivateAddressIsCountedAsForwarded()
    {
        await using var app = await StartApp();
        using var metrics = new MetricsRecorder(app);

        // Два прокси, а ForwardLimit = 1: берём адрес внутреннего прокси
        _ = await Send(app, "/", remoteIp: "10.0.0.5", forwardedFor: "8.8.8.8, 10.0.0.7");

        metrics.Measurements.ShouldBe(["true"]);
    }

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    [InlineData("/metrics")]
    public async Task InfrastructurePathsAreNotCounted(string path)
    {
        await using var app = await StartApp();
        using var metrics = new MetricsRecorder(app);

        _ = await Send(app, path, remoteIp: "10.0.0.5");

        metrics.Measurements.ShouldBeEmpty();
    }

    [Fact]
    public async Task RequestWithoutRemoteAddressIsNotCounted()
    {
        await using var app = await StartApp();
        using var metrics = new MetricsRecorder(app);

        _ = await Send(app, "/");

        metrics.Measurements.ShouldBeEmpty();
    }

    private static Task<HttpContext> Send(WebApplication app, string path, string? remoteIp = null, string? forwardedFor = null)
        => app.GetTestServer().SendAsync(context =>
        {
            context.Request.Path = path;
            if (remoteIp is not null)
            {
                context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
            }
            if (forwardedFor is not null)
            {
                context.Request.Headers["X-Forwarded-For"] = forwardedFor;
            }
        });

    private static async Task<WebApplication> StartApp()
    {
        var builder = WebApplication.CreateBuilder();
        _ = builder.WebHost.UseTestServer();
        _ = builder.Services.AddJoinForwardedHeaders();

        var app = builder.Build();
        _ = app.Use((context, next) =>
        {
            context.Response.Headers[RemoteIpHeader] = context.Connection.RemoteIpAddress?.ToString();
            return next(context);
        });
        await app.StartAsync();
        return app;
    }

    /// <summary>
    /// Слушает счётчик только своего приложения: meter создан через его <see cref="IMeterFactory"/>,
    /// поэтому параллельные тесты друг другу не мешают.
    /// </summary>
    private sealed class MetricsRecorder : IDisposable
    {
        private readonly MeterListener listener = new();
        private readonly List<string?> measurements = [];

        public MetricsRecorder(WebApplication app)
        {
            var meterFactory = app.Services.GetRequiredService<IMeterFactory>();
            listener.InstrumentPublished = (instrument, l) =>
            {
                if (instrument.Meter.Scope == meterFactory
                    && instrument.Meter.Name == WebInfrastructureMetrics.MeterName
                    && instrument.Name == PrivateNetworkRequestMetrics.CounterName)
                {
                    l.EnableMeasurementEvents(instrument);
                }
            };
            listener.SetMeasurementEventCallback<long>((_, value, tags, _) =>
            {
                value.ShouldBe(1);
                foreach (var tag in tags)
                {
                    if (tag.Key == PrivateNetworkRequestMetrics.ForwardedTagName)
                    {
                        lock (measurements)
                        {
                            measurements.Add(tag.Value as string);
                        }
                    }
                }
            });
            listener.Start();
        }

        public IReadOnlyList<string?> Measurements
        {
            get
            {
                lock (measurements)
                {
                    return [.. measurements];
                }
            }
        }

        public void Dispose() => listener.Dispose();
    }
}
