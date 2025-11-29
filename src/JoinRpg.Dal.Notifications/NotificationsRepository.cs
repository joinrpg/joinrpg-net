using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore.Storage;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository : INotificationRepository
{
    private readonly Counter<int> successRaceCounter;
    private readonly NotificationsDataDbContext dbContext;

    private readonly IExecutionStrategy executionStrategy;

    private readonly string lockRequestSql;

    public NotificationsRepository(
        NotificationsDataDbContext dbContext,
        IMeterFactory meterFactory)
    {
        this.dbContext = dbContext;
        var meter = meterFactory.Create("JoinRpg.Dal.Notifications.Repository");
        successRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_success");
        executionStrategy = dbContext.Database.CreateExecutionStrategy();

        lockRequestSql = BuildLockRequestSql();
    }

    private string BuildLockRequestSql()
    {
        var channelsTableName = dbContext.NotificationMessageChannels.EntityType.GetTableName();
        Debug.Assert(channelsTableName is not null);
        var channelStatusPropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.NotificationMessageStatus))
            .GetColumnName();
        Debug.Assert(channelStatusPropName is not null);
        var channelTypePropName = dbContext.NotificationMessageChannels.EntityType
            .GetProperty(nameof(NotificationMessageChannel.Channel))
            .GetColumnName();

        return $"SELECT * FROM {channelsTableName} ch"
            + $"\nWHERE {channelTypePropName} = {{0}} AND {channelStatusPropName} = {{1}}"
            + "\nLIMIT 1"
            + "\nLOCK FOR UPDATE SKIP LOCKED";
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
                NotificationMessageChannels = [.. notification.Channels.Select(ToNotificationMessageChannel)],
            };
            _ = dbContext.Notifications.Add(message);
        }
        _ = await dbContext.SaveChangesAsync();
    }

    private static NotificationMessageChannel ToNotificationMessageChannel(NotificationAddress c)
    {
        (var channel, var specificValue) = c;
        return new NotificationMessageChannel()
        {
            Channel = channel,
            ChannelSpecificValue = specificValue,
            NotificationMessageStatus = NotificationMessageStatus.Queued,
            NotificationMessage = null!,
        };
    }

    Task INotificationRepository.MarkSendingFailed(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Failed);

    Task INotificationRepository.MarkEnqueued(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Queued);

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
            candidate.NotificationMessageChannelId,
            candidate.Channel,
            candidate.NotificationMessageStatus,
            NotificationMessageStatus.Sending);

        successRaceCounter.Add(1);

        return CreateNotificationMessageDto(candidate);
    }

    Task<TargetedNotificationMessageForRecipient?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        return executionStrategy.ExecuteInTransactionAsync(
            new SelectMessageData(channel),
            InternalSelectNextNotificationAsync,
            (_, _) => Task.FromResult(false));
    }

    private static TargetedNotificationMessageForRecipient CreateNotificationMessageDto(NotificationMessageChannel candidate)
    {
        return new TargetedNotificationMessageForRecipient(new DataModel.MarkdownString(candidate.NotificationMessage.Body),
                                               new(candidate.NotificationMessage.InitiatorUserId),
                                               candidate.NotificationMessage.Header,
                                               new(candidate.NotificationMessage.RecipientUserId),
                                               new NotificationAddress(candidate.Channel, candidate.ChannelSpecificValue));
    }

    private async Task SetStatus(int messageId, NotificationChannel channel, NotificationMessageStatus from, NotificationMessageStatus to)
    {
        var totalRows = await dbContext
            .NotificationMessageChannels
            .Where(ch => ch.NotificationMessageId == messageId)
            .ForChannelAndStatus(channel, from)
            .ExecuteUpdateAsync(ch => ch.SetProperty(x => x.NotificationMessageStatus, to));

        // Both cases are unexpected due to row-level lock and provided search criteria, so we can throw.
        switch (totalRows)
        {
            case 0:
                throw new DbUpdateConcurrencyException($"Failed to change status of channel {channel} from {from} to {to} of message {messageId}");
            case > 1:
                throw new DbUpdateConcurrencyException($"Unexpected number ({totalRows}) of updated notification channels {channel} from {from} to {to} of message {messageId}");
        }
    }
}
