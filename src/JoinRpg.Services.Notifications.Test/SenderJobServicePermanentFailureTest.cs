using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Test;

public class SenderJobServicePermanentFailureTest
{
    [Fact]
    public async Task PermanentFailure_MarksMessageFailedWithoutRetries()
    {
        var repository = new FakeNotificationRepository();
        var messageId = new NotificationId(84168);
        var message = new TargetedNotificationMessageForRecipient(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), "header", new UserIdentification(2), null, DateTimeOffset.UtcNow),
            NotificationAddress.Ui(),
            Attempts: 1, // первая же попытка: ретраев ещё полно, но повторять всё равно бессмысленно
            messageId);
        repository.Enqueue(message);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INotificationRepository>(repository);
        builder.Services.Configure<NotificationWorkerOptions>(o =>
        {
            o.MaxSubsequentFailures = 1;
            o.MaxCooldownPause = TimeSpan.Zero;
            o.EmptyPause = TimeSpan.FromMilliseconds(10);
        });
        builder.Services.AddSenderJob<PermanentFailureSenderJob>();

        using var host = builder.Build();
        await host.StartAsync();
        await Task.WhenAny(repository.ProcessedSignal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await host.StopAsync();

        repository.ProcessedSignal.Task.IsCompletedSuccessfully.ShouldBeTrue();
        repository.MarkEnqueuedCalls.ShouldBeEmpty(); // никаких ретраев
        repository.MarkSendingSucceededCalls.ShouldBeEmpty();
        repository.MarkSendingFailedCalls.ShouldHaveSingleItem().Id.ShouldBe(messageId);
    }

    private sealed class PermanentFailureSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.ShowInUi;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => true;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => Task.FromResult(SendingResult.PermanentUserFailure());
    }
}
