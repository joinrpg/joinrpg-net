using System.Diagnostics.Metrics;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.Common.WebInfrastructure;

public class HealthCheckMetricsPublisher : IHealthCheckPublisher
{
    private static readonly Meter meter = new("JoinRpg");
    private static readonly Histogram<double> healthCheckDuration = meter.CreateHistogram<double>(
        "healthcheck.duration",
        "seconds",
        "Duration of health check execution");

    private static readonly Counter<int> healthCheckStatus = meter.CreateCounter<int>(
        "healthcheck.status",
        description: "Health check status (0=Unhealthy, 1=Healthy, 2=Degraded)");

    public Task PublishAsync(HealthReport report, CancellationToken cancellationToken)
    {
        foreach (var entry in report.Entries)
        {
            // Публикуем продолжительность проверки
            healthCheckDuration.Record(
                entry.Value.Duration.TotalSeconds,
                new KeyValuePair<string, object?>("healthcheck.name", entry.Key));

            // Публикуем статус проверки
            var statusValue = entry.Value.Status switch
            {
                HealthStatus.Healthy => 1,
                HealthStatus.Degraded => 2,
                _ => 0 // Unhealthy
            };

            healthCheckStatus.Add(1,
                new KeyValuePair<string, object?>("healthcheck.name", entry.Key),
                new KeyValuePair<string, object?>("status", statusValue.ToString()));
        }

        // Также публикуем общий статус
        var overallStatus = report.Status switch
        {
            HealthStatus.Healthy => 1,
            HealthStatus.Degraded => 2,
            _ => 0
        };

        healthCheckStatus.Add(1,
            new KeyValuePair<string, object?>("healthcheck.name", "overall"),
            new KeyValuePair<string, object?>("status", overallStatus.ToString()));

        return Task.CompletedTask;
    }
}
