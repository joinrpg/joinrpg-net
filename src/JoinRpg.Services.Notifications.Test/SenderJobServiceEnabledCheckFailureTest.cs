using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Test;

public class SenderJobServiceEnabledCheckFailureTest
{
    // В .NET 11 упавший BackgroundService останавливает хост целиком:
    // ошибка при проверке Enabled не должна ронять приложение.
    [Fact]
    public async Task EnabledCheckFailure_DoesNotStopHost()
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INotificationRepository>(new FakeNotificationRepository());
        builder.Services.AddSenderJob<ThrowingEnabledSenderJob>();

        using var host = builder.Build();
        await host.StartAsync();

        var job = host.Services.GetServices<IHostedService>().OfType<SenderJobService<ThrowingEnabledSenderJob>>().Single();
        await job.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        job.ExecuteTask.IsCompletedSuccessfully.ShouldBeTrue();
        host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested.ShouldBeFalse();

        await host.StopAsync();
    }

    private sealed class ThrowingEnabledSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.ShowInUi;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => throw new InvalidOperationException("Не удалось прочитать конфигурацию отправителя");

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => throw new InvalidOperationException("Отправитель не должен вызываться");
    }
}
