using System.Collections.Concurrent;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.DomainTypes.Interfaces;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Очередь уведомлений в памяти — общая для всех тестовых проектов.
/// </summary>
/// <remarks>
/// <para>
/// Для тестов отправки в неё кладут сообщения через <see cref="Enqueue"/> и смотрят, какие пометки
/// статуса поставил отправщик. Чтение уведомлений проекта отдаёт добавленные через
/// <see cref="AddClaimNotification"/> (по умолчанию — ничего) и записывает, о чём спросили. Всё ненастроенное бросает <see cref="NotSupportedException"/> намеренно — чтобы
/// поход за незапланированными данными был виден в тесте, а не подменялся пустышкой.
/// </para>
/// <para>
/// Приложению целиком, когда база уведомлений не поднята, подставлять <see cref="CreateNullObject"/>:
/// он молча принимает всё. Коллекции потокобезопасные: там экземпляр живёт синглтоном.
/// </para>
/// </remarks>
/// <param name="queueLengths">Что отдавать на <see cref="GetQueueLengths"/>.</param>
public sealed class FakeNotificationRepository(IReadOnlyDictionary<NotificationChannel, int>? queueLengths = null) : INotificationRepository
{
    private readonly ConcurrentQueue<TargetedNotificationMessageForRecipient> queue = new();
    private readonly ConcurrentQueue<(NotificationId Id, NotificationChannel Channel, DateTimeOffset SendAfter, int? Attempts)> markEnqueuedCalls = new();
    private readonly ConcurrentQueue<(NotificationId Id, NotificationChannel Channel)> markSendingFailedCalls = new();
    private readonly ConcurrentQueue<(NotificationId Id, NotificationChannel Channel)> markSendingSucceededCalls = new();
    private readonly ConcurrentQueue<(ProjectIdentification ProjectId, DateTimeOffset CreatedBefore)> getClaimNotificationsForProjectCalls = new();
    private readonly ConcurrentQueue<NotificationMessageForRecipient> claimNotifications = new();
    private readonly ConcurrentQueue<(ClaimIdentification ClaimId, DateTimeOffset CreatedBefore)> getClaimNotificationsForClaimCalls = new();

    private bool isNullObject;

    /// <summary>
    /// Пустая очередь, которая молча принимает всё, — для хоста приложения без базы уведомлений.
    /// </summary>
    public static FakeNotificationRepository CreateNullObject() => new(new Dictionary<NotificationChannel, int>()) { isNullObject = true };

    public IReadOnlyCollection<(NotificationId Id, NotificationChannel Channel, DateTimeOffset SendAfter, int? Attempts)> MarkEnqueuedCalls => markEnqueuedCalls;

    public IReadOnlyCollection<(NotificationId Id, NotificationChannel Channel)> MarkSendingFailedCalls => markSendingFailedCalls;

    public IReadOnlyCollection<(NotificationId Id, NotificationChannel Channel)> MarkSendingSucceededCalls => markSendingSucceededCalls;

    public IReadOnlyCollection<(ProjectIdentification ProjectId, DateTimeOffset CreatedBefore)> GetClaimNotificationsForProjectCalls => getClaimNotificationsForProjectCalls;

    /// <summary>
    /// Завершается на первой же пометке статуса — чтобы тест дождался, пока отправщик обработает сообщение.
    /// Для тестов, где отправщик обрабатывает больше одного сообщения, не годится.
    /// </summary>
    public TaskCompletionSource ProcessedSignal { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public void Enqueue(TargetedNotificationMessageForRecipient message) => queue.Enqueue(message);

    public Task<TargetedNotificationMessageForRecipient?> SelectNextNotificationForSending(NotificationChannel channel)
        => Task.FromResult(queue.TryDequeue(out var message) ? message : null);

    public Task MarkSendingSucceeded(NotificationId id, NotificationChannel channel)
    {
        markSendingSucceededCalls.Enqueue((id, channel));
        _ = ProcessedSignal.TrySetResult();
        return Task.CompletedTask;
    }

    public Task MarkSendingFailed(NotificationId id, NotificationChannel channel)
    {
        markSendingFailedCalls.Enqueue((id, channel));
        _ = ProcessedSignal.TrySetResult();
        return Task.CompletedTask;
    }

    public Task MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts = null)
    {
        markEnqueuedCalls.Enqueue((id, channel, sendAfter, attempts));
        _ = ProcessedSignal.TrySetResult();
        return Task.CompletedTask;
    }

    public Task InsertNotifications(NotificationMessageCreateDto[] notifications)
        => isNullObject ? Task.CompletedTask : throw new NotSupportedException();

    public Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(
        UserIdentification userId,
        NotificationChannel notificationChannel,
        KeySetPagination pagination)
        => isNullObject ? Task.FromResult<IReadOnlyCollection<NotificationHistoryDto>>([]) : throw new NotSupportedException();

    public Task<IReadOnlyDictionary<NotificationChannel, int>> GetQueueLengths()
        => Task.FromResult(queueLengths ?? throw new NotSupportedException());

    public Task<IReadOnlyCollection<NotificationLogItemDto>> GetNotificationLog(UserIdentification? recipient, NotificationChannel? channel, int limit)
        => isNullObject ? Task.FromResult<IReadOnlyCollection<NotificationLogItemDto>>([]) : throw new NotSupportedException();

    /// <summary>Добавляет уведомление по заявке — его отдаст <see cref="GetClaimNotificationsForProject"/>.</summary>
    public void AddClaimNotification(NotificationMessageForRecipient notification) => claimNotifications.Enqueue(notification);

    public Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForProject(ProjectIdentification projectId, DateTimeOffset createdBefore)
    {
        getClaimNotificationsForProjectCalls.Enqueue((projectId, createdBefore));
        return Task.FromResult<IReadOnlyCollection<NotificationMessageForRecipient>>(
            [.. claimNotifications.Where(n => n.EntityReference?.ProjectId == projectId && n.CreatedAt < createdBefore)]);
    }

    public IReadOnlyCollection<(ClaimIdentification ClaimId, DateTimeOffset CreatedBefore)> GetClaimNotificationsForClaimCalls => getClaimNotificationsForClaimCalls;

    public Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForClaim(ClaimIdentification claimId, DateTimeOffset createdBefore)
    {
        getClaimNotificationsForClaimCalls.Enqueue((claimId, createdBefore));
        return Task.FromResult<IReadOnlyCollection<NotificationMessageForRecipient>>(
            [.. claimNotifications.Where(n => ClaimOf(n.EntityReference) == claimId && n.CreatedAt < createdBefore)]);
    }

    private static ClaimIdentification? ClaimOf(IProjectEntityId? reference) => reference switch
    {
        ClaimIdentification claim => claim,
        ClaimCommentIdentification comment => comment.ClaimId,
        FinanceOperationIdentification operation => operation.ClaimId,
        _ => null,
    };
}
