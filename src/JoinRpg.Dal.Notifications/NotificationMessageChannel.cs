namespace JoinRpg.Dal.Notifications;

[Index(nameof(Channel), nameof(NotificationMessageChannelId), IsUnique = true)]
[Index(nameof(NotificationMessageStatus), nameof(Channel), nameof(NotificationMessageChannelId))]
internal class NotificationMessageChannel
{
    public int NotificationMessageChannelId { get; set; }
    public required NotificationMessage NotificationMessage { get; set; }

    public int NotificationMessageId { get; set; }

    public required NotificationChannel Channel { get; set; }
    [MaxLength(1024)]
    public required string ChannelSpecificValue { get; set; }
    public required NotificationMessageStatus NotificationMessageStatus { get; set; }
}
