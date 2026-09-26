using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;

namespace JoinRpg.Services.Notifications.Test;

/// <summary>
/// Очередь уведомлений в памяти: отдаёт заранее положенные сообщения и записывает все вызовы пометок статуса.
/// </summary>
internal sealed class FakeNotificationRepository : INotificationRepository
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
