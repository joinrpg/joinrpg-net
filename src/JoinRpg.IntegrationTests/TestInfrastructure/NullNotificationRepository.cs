using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// No-op implementation of INotificationRepository for tests where notifications DB is not configured.
/// </summary>
internal class NullNotificationRepository : INotificationRepository
{
    public Task InsertNotifications(NotificationMessageCreateDto[] notifications) => Task.CompletedTask;

    public Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel)
        => Task.FromResult<TargetedNotificationMessageForRecipient?>(null);

    public Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel) => Task.CompletedTask;

    public Task MarkSendingFailed(NotificationId id, NotificationChannel channel) => Task.CompletedTask;

    public Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null)
        => Task.CompletedTask;

    public Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(
        UserIdentification userId,
        NotificationChannel notificationChannel,
        KeySetPagination pagination)
        => Task.FromResult<IReadOnlyCollection<NotificationHistoryDto>>(Array.Empty<NotificationHistoryDto>());
}
