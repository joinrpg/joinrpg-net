using System.Diagnostics.Metrics;

namespace JoinRpg.Dal.Notifications;

internal class NotificationsRepository : INotificationRepository
{
    private readonly Counter<int> lostRaceCounter;
    private readonly Counter<int> successRaceCounter;
    private readonly NotificationsDataDbContext dbContext;
    private readonly ILogger<NotificationsRepository> logger;

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
    }

    async Task INotificationRepository.InsertNotifications(NotificationMessageDto[] notifications)
    {
        foreach (var x in notifications)
        {
            var message = new NotificationMessage()
            {
                Body = x.Body.Contents!,
                Header = x.Header,
                InitiatorAddress = x.InitiatorAddress,
                InitiatorUserId = x.Initiator.Value,
                RecepientUserId = x.Recepient.Value,
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
    async Task INotificationRepository.MarkSendFailed(NotificationId id, NotificationChannel channel)
    {
        if (!await TrySetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Failed))
        {
            logger.LogWarning("Notification {notificationId} for channel {channel} failed to set status to failed", id, channel);
        }
    }

    async Task INotificationRepository.MarkSendSuccess(NotificationId id, NotificationChannel channel)
    {
        if (!await TrySetStatus(id.Value, channel, from: NotificationMessageStatus.Sending, to: NotificationMessageStatus.Sent))
        {
            logger.LogWarning("Notification {notificationId} for channel {channel} failed to set status to success", id, channel);
        }
    }
    async Task<(NotificationId Id, NotificationMessageDto Message)?> INotificationRepository.SelectNextNotificationForSending(NotificationChannel channel)
    {
        var tryCount = 0;
        while (tryCount < 5)
        {
            var candidate =
                await dbContext.NotificationMessageChannels
                .Include(c => c.NotificationMessage)
                .ThenInclude(m => m.NotificationMessageChannels)
                .ForChannelAndStatus(channel, NotificationMessageStatus.Queued)
                .FirstOrDefaultAsync();

            if (candidate is null)
            {
                return null;
            }

            if (await TrySetStatus(candidate.NotificationMessageId, channel, from: NotificationMessageStatus.Queued, to: NotificationMessageStatus.Sending))
            {
                successRaceCounter.Add(1);
                return (new(candidate.NotificationMessageId), CreateNotificationMessageDto(candidate.NotificationMessage));
            }

            // lost race

            logger.LogDebug("Lost race when try to acquire candidate for sending!");
            lostRaceCounter.Add(1);
            await Task.Delay(Random.Shared.Next(100 * tryCount));
            tryCount++;
        }
        logger.LogWarning("Constantly losing race...");
        return null;
    }

    private static NotificationMessageDto CreateNotificationMessageDto(NotificationMessage candidate)
    {

        return new NotificationMessageDto
        {
            Body = new DataModel.MarkdownString(candidate.Body),
            Channels = [.. candidate.NotificationMessageChannels.Select(c => new NotificationChannelDto(c.Channel, c.ChannelSpecificValue))],
            Header = candidate.Header,
            Initiator = new(candidate.InitiatorUserId),
            InitiatorAddress = new(candidate.InitiatorAddress),
            Recepient = new(candidate.RecepientUserId),
        };
    }

    private async Task<bool> TrySetStatus(int id, NotificationChannel channel, NotificationMessageStatus from, NotificationMessageStatus to)
    {
        var totalRows = await dbContext
            .NotificationMessageChannels
            .Where(n => n.NotificationMessageId == id)
            .ForChannelAndStatus(channel, from)
            .ExecuteUpdateAsync(ch => ch.SetProperty(x => x.NotificationMessageStatus, to));
        return totalRows switch
        {
            0 => false,
            1 => true,
            _ => throw new InvalidOperationException("Unexpected — too many rows updated")
        };
    }
}
