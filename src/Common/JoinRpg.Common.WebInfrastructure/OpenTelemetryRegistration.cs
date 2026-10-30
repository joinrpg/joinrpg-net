using System.Reflection;
using JoinRpg.Common.WebInfrastructure.Job;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JoinRpg.Common.WebInfrastructure;

public static class OpenTelemetryRegistration
{
    public static void AddJoinOpenTelemetry(this IServiceCollection services, string serviceName, params IEnumerable<string> sourceNames)
    {
        _ = services.AddOpenTelemetry()
            .ConfigureResource(builder
            => builder.AddService(serviceName: serviceName, serviceVersion: Assembly.GetEntryAssembly()!.GetName().Version?.ToString()))
            .WithTracing(tracing =>
            {
                foreach (var sourceName in sourceNames)
                {
                    tracing.AddSource(sourceName);
                }
                // Активности фоновых джоб (AddMidnightJobs) — источник подключаем всегда, хосту передавать его не нужно.
                tracing.AddSource(BackgroundServiceActivity.ActivitySourceName);
                tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    // Общий meter приложений JoinRpg: в него пишут SenderJobService, NotificationsRepository и др.,
                    // независимо от имени приложения
                    .AddMeter("JoinRpg")
                    .AddMeter(WebInfrastructureMetrics.MeterName)
                    // Metrics provides by ASP.NET Core in .NET 8
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddPrometheusExporter();
            });
    }
}
