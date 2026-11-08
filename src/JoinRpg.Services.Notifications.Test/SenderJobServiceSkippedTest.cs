using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Test;

public class SenderJobServiceSkippedTest
{
    [Fact]
    public async Task Skipped_MarksMessageFailedWithoutRetriesAndDoesNotCountAsFailure()
    {
        var repository = new FakeNotificationRepository();
        var skippedMessageId = new NotificationId(84169);
        var followingMessageId = new NotificationId(84170);
        repository.Enqueue(CreateMessage(skippedMessageId));
        repository.Enqueue(CreateMessage(followingMessageId));

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INotificationRepository>(repository);
        builder.Services.Configure<NotificationWorkerOptions>(o =>
        {
            o.MaxSubsequentFailures = 1;
            o.MaxCooldownPause = TimeSpan.Zero;
            o.EmptyPause = TimeSpan.FromMilliseconds(10);
        });
        builder.Services.AddSenderJob<SkippedSenderJob>();

        using var host = builder.Build();
        await host.StartAsync();
        await Task.WhenAny(WaitForSuccess(repository), Task.Delay(TimeSpan.FromSeconds(5)));
        await host.StopAsync();

        // Skipped: помечено неотправленным без ретраев
        repository.MarkSendingFailedCalls.ShouldHaveSingleItem().Id.ShouldBe(skippedMessageId);
        repository.MarkEnqueuedCalls.ShouldBeEmpty();
        // Пропуск не считается ошибкой: следующее сообщение обработано (иначе был бы кулдаун и остановка)
        repository.MarkSendingSucceededCalls.ShouldHaveSingleItem().Id.ShouldBe(followingMessageId);
    }

    private static TargetedNotificationMessageForRecipient CreateMessage(NotificationId messageId)
        => new(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), "header", new UserIdentification(2), null, DateTimeOffset.UtcNow),
            NotificationAddress.Ui(),
            Attempts: 0,
            messageId);

    private static async Task WaitForSuccess(FakeNotificationRepository repository)
    {
        while (repository.MarkSendingSucceededCalls.Count == 0)
        {
            await Task.Delay(10);
        }
    }

    private sealed class SkippedSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.ShowInUi;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => true;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => Task.FromResult(message.MessageId.Value == 84169
                ? SendingResult.Skipped("получатель не настроен")
                : SendingResult.Success());
    }
}
