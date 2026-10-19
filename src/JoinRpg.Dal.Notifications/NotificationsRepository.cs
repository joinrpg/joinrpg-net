using System.Diagnostics.Metrics;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Dal.CommonEfCore;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Interfaces;
using LinqKit;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository(
    NotificationsDataDbContext dbContext,
    ILogger<NotificationsRepository> logger
        ) : INotificationRepository
{
    private readonly IExecutionStrategy executionStrategy = dbContext.Database.CreateExecutionStrategy();

    private readonly string lockRequestSql = BuildLockRequestSql(dbContext);

    private static readonly Meter meter = new("JoinRpg");
    private static readonly Counter<int> successRaceCounter = meter.CreateCounter<int>("joinrpg.dal.notifications.repository.notifications_select_success");

    private static string BuildLockRequestSql(NotificationsDataDbContext dbContext)
    {
        var channelsTableName = dbContext.NotificationMessageChannels.EntityType.GetTableName();
        var channelStatusPropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.NotificationMessageStatus))
            .GetColumnName();
        var channelTypePropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.Channel))
            .GetColumnName();
        var momentPropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.SendAfter))
            .GetColumnName();

        return $"SELECT * FROM \"{channelsTableName}\" ch"
            + $"\nWHERE \"{channelTypePropName}\" = {{0}} AND \"{channelStatusPropName}\" = {{1}} AND CURRENT_TIMESTAMP > \"{momentPropName}\""
            + "\nLIMIT 1"
            + "\nFOR UPDATE SKIP LOCKED"
            ;
    }

    async Task INotificationRepository.InsertNotifications(NotificationMessageCreateDto[] notifications)
    {
        foreach (var notification in notifications)
        {
            var x = notification.Message;
            var message = new NotificationMessage()
            {
                Body = x.Body.Value,
                Header = x.Header,
                InitiatorUserId = x.Initiator.Value,
                RecipientUserId = x.Recipient.Value,
                NotificationMessageChannels = [.. notification.Channels.Select(c => ToNotificationMessageChannel(c, x.CreatedAt))],
                EntityReference = x.EntityReference?.ToString(),
                CreatedAt = x.CreatedAt,
                SkipSignature = x.SkipSignature,
            };
            _ = dbContext.Notifications.Add(message);
        }
        _ = await dbContext.SaveChangesAsync();
    }

    private static NotificationMessageChannel ToNotificationMessageChannel(NotificationAddress c, DateTimeOffset moment)
    {
        (var channel, var specificValue) = c;
        return new NotificationMessageChannel()
        {
            Channel = channel,
            ChannelSpecificValue = specificValue,
            NotificationMessageStatus = NotificationMessageStatus.Queued,
            NotificationMessage = null!, // Здесь это норм, т.к. сразу сохраняем
            Attempts = 0,
            SendAfter = moment,
        };
    }

    Task INotificationRepository.MarkSendingFailed(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Failed);

    Task INotificationRepository.MarkEnqueued(NotificationId id, NotificationChannel channel, DateTimeOffset sendAfter, int? attempts)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Queued, attempts, sendAfter);

    Task INotificationRepository.MarkSendingSucceeded(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Sent);

    private record struct SelectMessageData(NotificationChannel Channel);

    private async Task<TargetedNotificationMessageForRecipient?> InternalSelectNextNotificationAsync(SelectMessageData data, CancellationToken cancellationToken)
    {
        while (true)
        {
            var candidate = await dbContext.NotificationMessageChannels
                .FromSqlRaw(lockRequestSql, data.Channel, NotificationMessageStatus.Queued)
                .Include(static e => e.NotificationMessage)
                .AsNoTracking()
                .FirstOrDefaultAsync(cancellationToken);

            if (candidate is null)
            {
                // No messages left
                return null;
            }

            var dto = TryCreateTargetedNotificationMessageDto(candidate);

            if (dto is null)
            {
                // Битую строку не отправить никогда. Если просто бросить исключение, транзакция откатится,
                // строка останется в очереди и будет выбираться снова, останавливая весь канал (#5466).
                await SetStatus(
                    candidate.NotificationMessageId,
                    candidate.Channel,
                    from: candidate.NotificationMessageStatus,
                    to: NotificationMessageStatus.Failed);
                continue;
            }

            await SetStatus(
                candidate.NotificationMessageId,
                candidate.Channel,
                from: candidate.NotificationMessageStatus,
                to: NotificationMessageStatus.Sending,
                attempts: candidate.Attempts + 1);

            successRaceCounter.Add(1);

            return dto;
        }
    }

    Task<TargetedNotificationMessageForRecipient?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        return executionStrategy.ExecuteInTransactionAsync(
            new SelectMessageData(channel),
            InternalSelectNextNotificationAsync,
            (_, _) => Task.FromResult(false));
    }

    private TargetedNotificationMessageForRecipient? TryCreateTargetedNotificationMessageDto(NotificationMessageChannel candidate)
    {
        try
        {
            return new TargetedNotificationMessageForRecipient(CreateNotificationMessageDto(candidate.NotificationMessage),
                                                   new NotificationAddress(candidate.Channel, candidate.ChannelSpecificValue),
                                                   candidate.Attempts,
                                                   new NotificationId(candidate.NotificationMessageId)
                                                   );
        }
        catch (Exception e)
        {
            logger.LogError(e, "Failed to parse NotificationMessageChannel record from DB {notificationMessageChannelId}, marking it as failed", candidate.NotificationMessageChannelId);
            return null;
        }
    }

    private static NotificationMessageForRecipient CreateNotificationMessageDto(NotificationMessage message)
    {
        return new NotificationMessageForRecipient(new MarkdownString(message.Body),
                                               new(message.InitiatorUserId),
                                               message.Header,
                                               new(message.RecipientUserId),
                                               ProjectEntityIdParser.TryParseId(message.EntityReference, out var id) ? id : null,
                                               message.CreatedAt,
                                               message.SkipSignature
                                               );
    }

    private async Task SetStatus(
        int messageId,
        NotificationChannel channel,
        NotificationMessageStatus from,
        NotificationMessageStatus to,
        int? attempts = null,
        DateTimeOffset? sendAfter = null)
    {
        if (to == NotificationMessageStatus.Sending && !attempts.HasValue)
        {
            throw new ArgumentNullException(nameof(attempts));
        }

        Action<UpdateSettersBuilder<NotificationMessageChannel>> updateExpression
            = to switch
            {
                NotificationMessageStatus.Sending => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.Attempts, attempts.GetValueOrDefault()),
                NotificationMessageStatus.Queued when attempts.HasValue => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.SendAfter, sendAfter ?? DateTimeOffset.UtcNow)
                    .SetProperty(static channel => channel.Attempts, attempts.Value),
                NotificationMessageStatus.Queued => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to)
                    .SetProperty(static channel => channel.SendAfter, sendAfter ?? DateTimeOffset.UtcNow),
                _ => calls => calls
                    .SetProperty(static channel => channel.NotificationMessageStatus, to),
            };

        var totalRows = await dbContext
            .NotificationMessageChannels
            .Where(ch => ch.NotificationMessageId == messageId)
            .ForChannelAndStatus(channel, from)
            .ExecuteUpdateAsync(updateExpression);

        // Both the following cases are unexpected due to row-level lock or provided search criteria, so we can throw.
        switch (totalRows)
        {
            case 0:
                throw new DbUpdateConcurrencyException($"Failed to change status of channel {channel} from {from} to {to} of message {messageId}");
            case > 1:
                throw new DbUpdateConcurrencyException($"Unexpected number ({totalRows}) of updated notification channels {channel} from {from} to {to} of message {messageId}");
        }
    }

    public async Task<IReadOnlyCollection<NotificationHistoryDto>> GetLastNotificationsForUser(UserIdentification userId, NotificationChannel notificationChannel, KeySetPagination pagination)
    {
        var query =
            from message in dbContext.Notifications.AsNoTracking().AsExpandableEFCore()
            where message.RecipientUserId == userId
            where message.NotificationMessageChannels.Any(c => c.Channel == notificationChannel)
            select message;

        query = query.ApplyPaginationEfCore(pagination, n => n.NotificationMessageId);

        var result = await query.ToListAsync();

        return [.. result.Select(x => new NotificationHistoryDto(new NotificationId(x.NotificationMessageId), CreateNotificationMessageDto(x)))];
    }

    public async Task<IReadOnlyDictionary<NotificationChannel, int>> GetQueueLengths()
    {
        var counts = await dbContext.NotificationMessageChannels
            .Where(c => c.NotificationMessageStatus == NotificationMessageStatus.Queued)
            .GroupBy(c => c.Channel)
            .Select(g => new { Channel = g.Key, Count = g.Count() })
            .ToListAsync();

        return counts.ToDictionary(x => x.Channel, x => x.Count);
    }

    public async Task<IReadOnlyCollection<NotificationLogItemDto>> GetNotificationLog(UserIdentification? recipient, NotificationChannel? channel, int limit)
    {
        IQueryable<NotificationMessage> query;
        if (channel is not null)
        {
            // От строк канала по индексу (Channel, NotificationMessageChannelId): строки каналов вставляются вместе
            // с сообщением, так что их id растут в том же порядке. Через EXISTS от сообщений редкий канал (Telegram)
            // заставил бы просмотреть почти всю таблицу.
            var channelRows = dbContext.NotificationMessageChannels.Where(c => c.Channel == channel.Value);
            if (recipient is not null)
            {
                channelRows = channelRows.Where(c => c.NotificationMessage.RecipientUserId == recipient.Value);
            }
            var ids = channelRows
                .OrderByDescending(c => c.NotificationMessageChannelId)
                .Select(c => c.NotificationMessageId)
                .Take(limit);
            query = dbContext.Notifications.Where(m => ids.Contains(m.NotificationMessageId));
        }
        else
        {
            query = dbContext.Notifications;
            if (recipient is not null)
            {
                query = query.Where(m => m.RecipientUserId == recipient.Value);
            }
        }

        var result = await query
            .AsNoTracking()
            .OrderByDescending(m => m.NotificationMessageId)
            .Take(limit)
            .Include(m => m.NotificationMessageChannels.Where(c => channel == null || c.Channel == channel.Value))
            .ToListAsync();

        return [.. result.Select(m => new NotificationLogItemDto(
            new NotificationId(m.NotificationMessageId),
            CreateNotificationMessageDto(m),
            [.. m.NotificationMessageChannels
                .OrderBy(c => c.Channel)
                .Select(c => new NotificationDeliveryDto(c.Channel, c.ChannelSpecificValue, c.NotificationMessageStatus, c.Attempts, c.SendAfter))]))];
    }

    public async Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForProject(ProjectIdentification projectId, DateTimeOffset createdBefore)
    {
        var (claimPrefix, commentPrefix, financePrefix) = GetClaimReferencePrefixes(projectId);
        // Npgsql пишет в timestamptz только DateTimeOffset со смещением 0.
        var cutoff = createdBefore.ToUniversalTime();

        // StartsWith → LIKE 'ClaimId(12-%': идёт по индексу на EntityReference (text_pattern_ops).
        var result = await dbContext.Notifications
            .AsNoTracking()
            .Where(m => m.EntityReference!.StartsWith(claimPrefix)
                || m.EntityReference!.StartsWith(commentPrefix)
                || m.EntityReference!.StartsWith(financePrefix))
            .Where(m => m.CreatedAt < cutoff)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.NotificationMessageId)
            .ToListAsync();

        return [.. result.Select(CreateNotificationMessageDto)];
    }

    public async Task<IReadOnlyCollection<NotificationMessageForRecipient>> GetClaimNotificationsForClaim(ClaimIdentification claimId, DateTimeOffset createdBefore)
    {
        var (claimReference, commentPrefix, financePrefix) = GetSingleClaimReferences(claimId);
        var cutoff = createdBefore.ToUniversalTime();

        // Сама заявка — точным совпадением (иначе «ClaimId(1-5» поймала бы заявку 50), остальное — префиксом
        // с завершающим «-» по тому же индексу.
        var result = await dbContext.Notifications
            .AsNoTracking()
            .Where(m => m.EntityReference == claimReference
                || m.EntityReference!.StartsWith(commentPrefix)
                || m.EntityReference!.StartsWith(financePrefix))
            .Where(m => m.CreatedAt < cutoff)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.NotificationMessageId)
            .ToListAsync();

        return [.. result.Select(CreateNotificationMessageDto)];
    }

    /// <summary>
    /// Ссылки одной заявки: «ClaimId(12-345)» целиком, префиксы «ClaimCommentId(12-345-» и «FinanceOperationId(12-345-».
    /// Берутся из ToString() самих идентификаторов, чтобы не разойтись с форматом.
    /// </summary>
    internal static (string Claim, string CommentPrefix, string FinancePrefix) GetSingleClaimReferences(ClaimIdentification claimId)
    {
        return (
            claimId.ToString(),
            UpToLastDash(new ClaimCommentIdentification(claimId, 0).ToString()),
            UpToLastDash(new FinanceOperationIdentification(claimId, 0).ToString()));

        static string UpToLastDash(string reference) => reference[..(reference.LastIndexOf('-') + 1)];
    }

    /// <summary>
    /// Префиксы ссылок на заявки проекта: «ClaimId(12-», «ClaimCommentId(12-», «FinanceOperationId(12-».
    /// Берутся из ToString() самих идентификаторов, чтобы не разойтись с форматом.
    /// </summary>
    internal static (string Claim, string Comment, string Finance) GetClaimReferencePrefixes(ProjectIdentification projectId)
    {
        var claim = new ClaimIdentification(projectId, 0);
        return (
            UpToFirstDash(claim.ToString()),
            UpToFirstDash(new ClaimCommentIdentification(claim, 0).ToString()),
            UpToFirstDash(new FinanceOperationIdentification(claim, 0).ToString()));

        static string UpToFirstDash(string reference) => reference[..(reference.IndexOf('-') + 1)];
    }
}
