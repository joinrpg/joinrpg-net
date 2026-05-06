using System.Diagnostics.Metrics;
using JoinRpg.IntegrationTest.TestInfrastructure;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace JoinRpg.IntegrationTest.Scenarios;

public class HealthCheckMetricsTests : IClassFixture<JoinApplicationFactory>
{
    private readonly IServiceProvider _serviceProvider;

    public HealthCheckMetricsTests(JoinApplicationFactory applicationFactory)
    {
        // Ensure server is initialized
        _ = applicationFactory.CreateClient();
        _serviceProvider = applicationFactory.Services;
    }

    [Fact]
    public void HealthCheckMetricsPublisher_RegistersMetrics()
    {
        // Arrange
        var publisher = _serviceProvider.GetRequiredService<IHealthCheckPublisher>();
        var meterListener = new MeterListener();
        var durationMeasurements = new List<(string name, double value)>();
        var statusMeasurements = new List<(string name, int value, KeyValuePair<string, object?>[] tags)>();

        meterListener.InstrumentPublished = (instrument, listener) =>
        {
            if (instrument.Meter.Name == "JoinRpg" &&
                (instrument.Name == "healthcheck.duration" || instrument.Name == "healthcheck.status"))
            {
                listener.EnableMeasurementEvents(instrument);
            }
        };

        meterListener.SetMeasurementEventCallback<double>((instrument, value, tags, state) =>
        {
            if (instrument.Name == "healthcheck.duration")
            {
                var tagList = tags.ToArray();
                var nameTag = tagList.FirstOrDefault(t => t.Key == "healthcheck.name");
                durationMeasurements.Add((nameTag.Value?.ToString() ?? "", value));
            }
        });

        meterListener.SetMeasurementEventCallback<int>((instrument, value, tags, state) =>
        {
            if (instrument.Name == "healthcheck.status")
            {
                statusMeasurements.Add((instrument.Name, value, tags.ToArray()));
            }
        });

        meterListener.Start();

        try
        {
            var report = new HealthReport(new Dictionary<string, HealthReportEntry>
            {
                ["test-check"] = new(
                    HealthStatus.Healthy,
                    "Test check",
                    TimeSpan.FromSeconds(1.5),
                    null,
                    null),
                ["another-check"] = new(
                    HealthStatus.Degraded,
                    "Another check",
                    TimeSpan.FromSeconds(0.5),
                    null,
                    null)
            }, totalDuration: TimeSpan.FromSeconds(2));

            // Act
            publisher.PublishAsync(report, CancellationToken.None).GetAwaiter().GetResult();

            // Allow some time for metrics to be recorded
            Thread.Sleep(100);

            // Force collection
            meterListener.RecordObservableInstruments();

            // Assert
            durationMeasurements.ShouldHaveSingleItem();
            var duration = durationMeasurements[0];
            duration.name.ShouldBe("test-check");
            duration.value.ShouldBe(1.5);

            // We expect two status entries (one per check) plus overall status
            statusMeasurements.Count.ShouldBe(3);

            var testStatus = statusMeasurements.First(s => s.tags.Any(t => t.Key == "healthcheck.name" && t.Value as string == "test-check"));
            testStatus.value.ShouldBe(1); // Healthy = 1

            var anotherStatus = statusMeasurements.First(s => s.tags.Any(t => t.Key == "healthcheck.name" && t.Value as string == "another-check"));
            anotherStatus.value.ShouldBe(2); // Degraded = 2

            var overallStatus = statusMeasurements.First(s => s.tags.Any(t => t.Key == "healthcheck.name" && t.Value as string == "overall"));
            overallStatus.value.ShouldBe(2); // Overall status = Degraded (since one check is degraded)
        }
        finally
        {
            meterListener.Dispose();
        }
    }
}
