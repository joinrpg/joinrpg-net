using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Test;

public class SenderJobServiceDisabledSenderTest
{
    [Fact]
    public async Task DisabledSender_WithNonEmptyQueue_DoesNotStopHost()
    {
        var repository = new FakeNotificationRepository();
        repository.Enqueue(new TargetedNotificationMessageForRecipient(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), "header", new UserIdentification(2), null, DateTimeOffset.UtcNow),
            NotificationAddress.Ui(),
            Attempts: 0,
            new NotificationId(42)));

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INotificationRepository>(repository);
        builder.Services.AddSenderJob<DisabledSenderJob>();

        using var host = builder.Build();
        await host.StartAsync();

        var job = host.Services.GetServices<IHostedService>().OfType<SenderJobService<DisabledSenderJob>>().Single();
        await job.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(5));

        job.ExecuteTask.IsCompletedSuccessfully.ShouldBeTrue();
        host.Services.GetRequiredService<IHostApplicationLifetime>().ApplicationStopping.IsCancellationRequested.ShouldBeFalse();
        repository.MarkEnqueuedCalls.ShouldBeEmpty();
        repository.MarkSendingFailedCalls.ShouldBeEmpty();
        repository.MarkSendingSucceededCalls.ShouldBeEmpty();

        await host.StopAsync();
    }

    private sealed class DisabledSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.ShowInUi;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => false;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => throw new InvalidOperationException("Выключенный отправитель не должен вызываться");
    }
}
