using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using JoinRpg.Common.WebInfrastructure;
using JoinRpg.Data.Write.Interfaces.Notifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JoinRpg.Services.Notifications;

/// <summary>
/// Периодически опрашивает длину очереди уведомлений по каналам и публикует её как метрику.
/// </summary>
internal class NotificationQueueMetricsPublisher(
    IServiceProvider serviceProvider,
    ILogger<NotificationQueueMetricsPublisher> logger,
    IOptions<NotificationWorkerOptions> workerOptions,
    IHostApplicationLifetime hostApplicationLifetime
    ) : BackgroundService
{
    private readonly NotificationWorkerOptions WorkerOptions = workerOptions.Value;

    private static readonly Meter meter = new("JoinRpg");
    private static readonly ConcurrentDictionary<NotificationChannel, int> queueLengths = new();

    // Возврат CreateObservableGauge не сохраняем: инструмент живёт, пока жив meter, регистрация не требует хранения ссылки.
    static NotificationQueueMetricsPublisher()
        => _ = meter.CreateObservableGauge(
            "joinrpg.notifications.queue_length",
            () => queueLengths.Select(kv => new Measurement<int>(kv.Value, new KeyValuePair<string, object?>("channel", kv.Key.ToString()))),
            description: "Number of notification messages currently queued for sending, per channel.");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await hostApplicationLifetime.WaitForAppStartup(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var notificationRepository = scope.ServiceProvider.GetRequiredService<INotificationRepository>();
                var lengths = await notificationRepository.GetQueueLengths();

                foreach (var channel in Enum.GetValues<NotificationChannel>())
                {
                    queueLengths[channel] = lengths.GetValueOrDefault(channel);
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Ошибка при получении длины очереди уведомлений для метрик");
            }

            await Task.Delay(WorkerOptions.QueueMetricsPollInterval, stoppingToken);
        }
    }
}
