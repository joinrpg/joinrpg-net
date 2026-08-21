using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Services.Notifications.Senders;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace JoinRpg.Services.Notifications.Tests;

public class SenderJobServiceCommonFailureTest
{
    [Fact]
    public async Task CommonFailure_ReturnsMessageToQueue_WithoutConsumingAttempt()
    {
        var repository = new FakeNotificationRepository();
        var messageId = new NotificationId(42);
        var message = new TargetedNotificationMessageForRecipient(
            new NotificationMessageForRecipient(new MarkdownString("body"), new UserIdentification(1), "header", new UserIdentification(2), null, DateTimeOffset.UtcNow),
            NotificationAddress.Ui(),
            Attempts: 3,
            messageId);
        repository.Enqueue(message);

        var builder = Host.CreateApplicationBuilder();
        builder.Services.AddSingleton<INotificationRepository>(repository);
        builder.Services.Configure<NotificationWorkerOptions>(o =>
        {
            // Один сбой сразу переводит джобу в кулдаун, а нулевой MaxCooldownPause сразу его останавливает —
            // так итерация обработки одного сообщения завершается предсказуемо, без реальных задержек.
            o.MaxSubsequentFailures = 1;
            o.MaxCooldownPause = TimeSpan.Zero;
            o.EmptyPause = TimeSpan.FromMilliseconds(10);
        });
        builder.Services.AddSenderJob<CommonFailureSenderJob>();

        using var host = builder.Build();
        await host.StartAsync();
        await Task.WhenAny(repository.ProcessedSignal.Task, Task.Delay(TimeSpan.FromSeconds(5)));
        await host.StopAsync();

        repository.ProcessedSignal.Task.IsCompletedSuccessfully.ShouldBeTrue();
        repository.MarkSendingFailedCalls.ShouldBeEmpty();
        repository.MarkSendingSucceededCalls.ShouldBeEmpty();
        var call = repository.MarkEnqueuedCalls.ShouldHaveSingleItem();
        call.Id.ShouldBe(messageId);
        call.Channel.ShouldBe(NotificationChannel.ShowInUi);
        call.Attempts.ShouldBe(3); // общая ошибка не должна тратить попытку сообщения
    }

    private sealed class CommonFailureSenderJob : ISenderJob
    {
        public static NotificationChannel Channel => NotificationChannel.ShowInUi;

        public NotificationChannel InstanceChannel => Channel;

        public bool Enabled => true;

        public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
            => Task.FromResult(SendingResult.CommonFailure());
    }

    private sealed class FakeNotificationRepository : INotificationRepository
    {
        private readonly Queue<TargetedNotificationMessageForRecipient> queue = new();

        public List<(NotificationId Id, NotificationChannel Channel, DateTimeOffset SendAfter, int? Attempts)> MarkEnqueuedCalls { get; } = [];

        public List<(NotificationId Id, NotificationChannel Channel)> MarkSendingFailedCalls { get; } = [];

        public List<(NotificationId Id, NotificationChannel Channel)> MarkSendingSucceededCalls { get; } = [];

        public TaskCompletionSource ProcessedSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void Enqueue(TargetedNotificationMessageForRecipient message) => queue.Enqueue(message);

        public Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel)
            => Task.FromResult(queue.Count > 0 ? queue.Dequeue() : null);

        public Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel)
        {
            MarkSendingSucceededCalls.Add((id, channel));
            _ = ProcessedSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public Task MarkSendingFailed(NotificationId id, NotificationChannel channel)
        {
            MarkSendingFailedCalls.Add((id, channel));
            _ = ProcessedSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null)
        {
            MarkEnqueuedCalls.Add((id, channel, sendAfter, attempts));
            _ = ProcessedSignal.TrySetResult();
            return Task.CompletedTask;
        }

        public Task InsertNotifications(NotificationMessageCreateDto[] notifications) => throw new NotSupportedException();

        public Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(UserIdentification userId, NotificationChannel notificationChannel, KeySetPagination pagination)
            => throw new NotSupportedException();

        public Task<IReadOnlyDictionary<NotificationChannel, int>> GetQueueLengths() => throw new NotSupportedException();
    }
}
