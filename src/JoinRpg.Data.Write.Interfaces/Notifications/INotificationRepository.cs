using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Data.Write.Interfaces.Notifications;

public interface INotificationRepository
{
    Task InsertNotifications(NotificationMessageCreateDto[] notifications);

    /// <summary>
    /// Tries to lock and return next enqueued notification message by the provided channel.
    /// </summary>
    /// <param name="channel">Transmission channel.</param>
    /// <returns>An instance of <see cref="TargetedNotificationMessageForRecipient"/> class with message details, null otherwise.</returns>
    Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel);

    /// <summary>
    /// Marks the message being sent as successfully sent.
    /// </summary>
    Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel);

    /// <summary>
    /// Marks the message being sent as failed to send.
    /// </summary>
    Task MarkSendingFailed(NotificationId id, NotificationChannel channel);

    /// <summary>
    /// Returns the message being sent back to queue.
    /// </summary>
    Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null);

    Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(UserIdentification userId, NotificationChannel notificationChannel, KeySetPagination pagination);
}

public record NotificationMessageCreateDto(NotificationMessageForRecipient Message, IReadOnlyCollection<NotificationAddress> Channels);

public record NotificationHistoryDto(NotificationMessageForRecipient Message, IReadOnlyCollection<NotificationChannel> OtherChannels);
