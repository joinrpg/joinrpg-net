using System.Diagnostics.Metrics;
using System.Linq.Expressions;
using JoinRpg.Dal.CommonEfCore;
using JoinRpg.Interfaces;
using JoinRpg.PrimitiveTypes;
using LinqKit;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.EntityFrameworkCore.Storage;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository : INotificationRepository
{
    private readonly Counter<int> successRaceCounter;
    private readonly NotificationsDataDbContext dbContext;
    private readonly ILogger<NotificationsRepository> logger;
    private readonly IExecutionStrategy executionStrategy;

    private readonly string lockRequestSql;

    public NotificationsRepository(
        NotificationsDataDbContext dbContext,
        IMeterFactory meterFactory,
        ILogger<NotificationsRepository> logger
        )
    {
        this.dbContext = dbContext;
        this.logger = logger;
        var meter = meterFactory.Create("JoinRpg.Dal.Notifications.Repository");
        successRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_success");
        executionStrategy = dbContext.Database.CreateExecutionStrategy();

        lockRequestSql = BuildLockRequestSql();
    }

    private string BuildLockRequestSql()
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
                Body = x.Body.Contents!,
                Header = x.Header,
                InitiatorUserId = x.Initiator.Value,
                RecipientUserId = x.Recipient.Value,
                NotificationMessageChannels = [.. notification.Channels.Select(c => ToNotificationMessageChannel(c, x.CreatedAt))],
                EntityReference = x.EntityReference?.ToString(),
                CreatedAt = x.CreatedAt,
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

        await SetStatus(
            candidate.NotificationMessageId,
            candidate.Channel,
            from: candidate.NotificationMessageStatus,
            to: NotificationMessageStatus.Sending,
            attempts: candidate.Attempts + 1);

        successRaceCounter.Add(1);

        return CreateTargetedNotificationMessageDto(candidate);
    }

    Task<TargetedNotificationMessageForRecipient?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        return executionStrategy.ExecuteInTransactionAsync(
            new SelectMessageData(channel),
            InternalSelectNextNotificationAsync,
            (_, _) => Task.FromResult(false));
    }

    private TargetedNotificationMessageForRecipient CreateTargetedNotificationMessageDto(NotificationMessageChannel candidate)
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
            logger.LogError(e, "Failed to parse NotificationMessageChannel record from DB {notificationMessageChannelId}", candidate.NotificationMessageChannelId);
            throw;
        }
    }

    private static NotificationMessageForRecipient CreateNotificationMessageDto(NotificationMessage message)
    {
        return new NotificationMessageForRecipient(new DataModel.MarkdownString(message.Body),
                                               new(message.InitiatorUserId),
                                               message.Header,
                                               new(message.RecipientUserId),
                                               ProjectEntityIdParser.TryParseId(message.EntityReference, out var id) ? id : null,
                                               message.CreatedAt
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

        Expression<Func<SetPropertyCalls<NotificationMessageChannel>, SetPropertyCalls<NotificationMessageChannel>>> updateExpression
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
            from message in dbContext.Notifications.AsExpandableEFCore().Include(x => x.NotificationMessageChannels)
            where message.RecipientUserId == userId
            where message.NotificationMessageChannels.Any(c => c.Channel == notificationChannel)
            select message;

        query = query.ApplyPaginationEfCore(pagination, n => n.NotificationMessageId);

        var result = await query.ToListAsync();

        return [.. result.Select(x => new NotificationHistoryDto(CreateNotificationMessageDto(x), [.. x.NotificationMessageChannels.Select(c => c.Channel)]))];

    }
}
