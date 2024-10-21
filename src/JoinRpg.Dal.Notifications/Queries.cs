namespace JoinRpg.Dal.Notifications;

internal static class Queries
{
    public static IQueryable<NotificationMessageChannel> ForChannelAndStatus(
        this IQueryable<NotificationMessageChannel> query,
        NotificationChannel channel,
        NotificationMessageStatus status)
        => query
            .Where(n => n.Channel == channel)
            .Where(n => n.NotificationMessageStatus == status);

}
