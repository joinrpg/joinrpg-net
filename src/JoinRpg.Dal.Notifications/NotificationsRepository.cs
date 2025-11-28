using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.EntityFrameworkCore.Storage;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository : INotificationRepository
{
    private readonly Counter<int> lostRaceCounter;
    private readonly Counter<int> successRaceCounter;
    private readonly NotificationsDataDbContext dbContext;
    private readonly ILogger<NotificationsRepository> logger;

    private readonly IExecutionStrategy executionStrategy;

    private readonly string lockRequestSql;

    public NotificationsRepository(
        NotificationsDataDbContext dbContext,
        ILogger<NotificationsRepository> logger,
        IMeterFactory meterFactory)
    {
        this.dbContext = dbContext;
        this.logger = logger;
        var meter = meterFactory.Create("JoinRpg.Dal.Notifications.Repository");
        lostRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_lost_races");
        successRaceCounter = meter.CreateCounter<int>("joinRpg.dal.notifications.repository.notifications_select_success_races");
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
        foreach (var x in notifications)
        {
            var message = new NotificationMessage()
            {
                Body = x.Body.Contents!,
                Header = x.Header,
                InitiatorAddress = x.InitiatorAddress,
                InitiatorUserId = x.Initiator.Value,
                RecipientUserId = x.Recipient.Value,
                NotificationMessageChannels = [
                    .. x.Channels.Select(c => new NotificationMessageChannel()
                    {
                        Channel = c.Channel,
                        ChannelSpecificValue = c.ChannelSpecificValue,
                        NotificationMessageStatus = NotificationMessageStatus.Queued,
                        NotificationMessage = null!,
                    })
                    ]
            };
            _ = dbContext.Notifications.Add(message);
        }
        _ = await dbContext.SaveChangesAsync();
    }

    Task INotificationRepository.MarkSendingFailed(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Failed);

    Task INotificationRepository.MarkEnqueued(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Queued);

    Task INotificationRepository.MarkSendingSucceeded(NotificationId id, NotificationChannel channel)
        => SetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Sent);

    private record struct SelectMessageData(NotificationChannel Channel);

    private async Task<AddressedNotificationMessageDto?> InternalSelectNextNotificationAsync(SelectMessageData data, CancellationToken cancellationToken)
    {
        var candidate = await dbContext.NotificationMessageChannels
            .FromSqlRaw(lockRequestSql, data.Channel, NotificationMessageStatus.Queued)
            .Include(static e => e.NotificationMessage)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (candidate is null)
        {
            // No messages left
            lostRaceCounter.Add(1);
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

    Task<AddressedNotificationMessageDto?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        return executionStrategy.ExecuteInTransactionAsync(
            new SelectMessageData(channel),
            InternalSelectNextNotificationAsync,
            (_, _) => Task.FromResult(false));
    }

    private static AddressedNotificationMessageDto CreateNotificationMessageDto(NotificationMessageChannel candidate)
    {
        return new AddressedNotificationMessageDto
        {
            Id = new NotificationId(candidate.NotificationMessageId),
            Channel = new NotificationChannelDto(candidate.Channel, candidate.ChannelSpecificValue),
            Body = new DataModel.MarkdownString(candidate.NotificationMessage.Body),
            Header = candidate.NotificationMessage.Header,
            Initiator = new(candidate.NotificationMessage.InitiatorUserId),
            InitiatorAddress = new(candidate.NotificationMessage.InitiatorAddress),
            Recipient = new(candidate.NotificationMessage.RecipientUserId),
        };
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
