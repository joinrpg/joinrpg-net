using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;

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

    /// <summary>
    /// Returns the number of queued messages per channel.
    /// </summary>
    Task<IReadOnlyDictionary<NotificationChannel, int>> GetQueueLengths();

    /// <summary>
    /// Последние <paramref name="limit"/> уведомлений, новые сверху, — для лога в админке.
    /// </summary>
    /// <param name="recipient">Только уведомления этому пользователю; null — всем.</param>
    /// <param name="channel">Только уведомления, отправляемые по этому каналу; в <see cref="NotificationLogItemDto.Channels"/>
    /// тогда попадает только он. null — все каналы.</param>
    Task<IReadOnlyCollection<NotificationLogItemDto>> GetNotificationLog(UserIdentification? recipient, NotificationChannel? channel, int limit);

    /// <summary>
    /// Все уведомления по заявкам проекта (сама заявка, её комментарии, финансовые операции), созданные до
    /// <paramref name="createdBefore"/>, — строка на каждого получателя, по возрастанию времени.
    /// Для восстановления заявок после потери основной БД (DRP, #5481).
    /// </summary>
    /// <remarks>
    /// Ссылки ищутся в нынешнем формате (<c>ClaimId(12-345)</c>), он пишется с 2026-04-01. Более старые
    /// уведомления (<c>ClaimId(345, 12)</c> и т.п.) сюда не попадут — для бэкапа от мая они и не нужны.
    /// </remarks>
    Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForProject(ProjectIdentification projectId, DateTimeOffset createdBefore);

    /// <summary>
    /// То же, что <see cref="GetClaimNotificationsForProject"/>, но по одной заявке: сама заявка, её комментарии
    /// и финансовые операции. Для восстановления комментариев одной заявки (DRP, #5492).
    /// </summary>
    Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForClaim(ClaimIdentification claimId, DateTimeOffset createdBefore);
}

public record NotificationMessageCreateDto(NotificationMessageForRecipient Message, IReadOnlyCollection<NotificationAddress> Channels);

public record NotificationHistoryDto(NotificationMessageForRecipient Message, IReadOnlyCollection<NotificationChannel> OtherChannels);

public record NotificationLogItemDto(NotificationId Id, NotificationMessageForRecipient Message, IReadOnlyCollection<NotificationDeliveryDto> Channels);

/// <summary>
/// Состояние отправки уведомления по одному каналу.
/// </summary>
/// <param name="Address">Адрес в канале (email, chat id Telegram) как он лежит в базе. Строкой, а не
/// <see cref="NotificationAddress"/>: в базе бывают адреса, которые не разбираются (#5466), а лог должен показать и их.</param>
/// <param name="SendAfter">Не раньше какого момента будет следующая попытка.</param>
public record NotificationDeliveryDto(NotificationChannel Channel, string Address, NotificationMessageStatus Status, int Attempts, DateTimeOffset SendAfter);
