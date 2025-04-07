using System.Reflection;
using JoinRpg.Portal.Infrastructure.DailyJobs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace JoinRpg.Portal.Infrastructure;

public static class OpenTelemetryRegistration
{
    public static void AddJoinOpenTelemetry(this IServiceCollection services)
    {
        const string serviceName = "JoinRpg";
        _ = services.AddOpenTelemetry()
            .ConfigureResource(builder =>
            {

                builder.AddService(serviceName: serviceName, serviceVersion: Assembly.GetEntryAssembly()!.GetName().Version?.ToString());
            })
            .WithTracing(tracing =>
            {
                tracing.AddSource(BackgroundServiceActivity.ActivitySourceName);
                tracing
                .AddHttpClientInstrumentation()
                .AddAspNetCoreInstrumentation();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddMeter(serviceName)
                    // Metrics provides by ASP.NET Core in .NET 8
                    .AddMeter("Microsoft.AspNetCore.Hosting")
                    .AddPrometheusExporter();
            });
    }
}
