namespace JoinRpg.Dal.Notifications;

[Index(nameof(RecepientUserId))]
[Index(nameof(InitiatorUserId))]
internal class NotificationMessage
{
    public int NotificationMessageId { get; set; }
    [MaxLength(1024)]
    public required string Header { get; set; }
    public required string Body { get; set; }
    public required int InitiatorUserId { get; set; }
    [MaxLength(1024)]
    public required string InitiatorAddress { get; set; }

    public required int RecepientUserId { get; set; }

    public virtual HashSet<NotificationMessageChannel> NotificationMessageChannels { get; set; } = [];
}
