namespace JoinRpg.Dal.Notifications;

[Index(nameof(RecipientUserId))]
[Index(nameof(InitiatorUserId))]
internal class NotificationMessage
{
    public int NotificationMessageId { get; set; }
    [MaxLength(1024)]
    public required string Header { get; set; }
    public required string Body { get; set; }
    public required int InitiatorUserId { get; set; }

    public required int RecipientUserId { get; set; }

    /// <summary>
    /// Какая-то информация о связанной с уведомлением сущности, например проекте, заявке и т.д.
    /// </summary>
    public required string? EntityReference { get; set; }

    public required DateTimeOffset CreatedAt { get; set; }

    public HashSet<NotificationMessageChannel> NotificationMessageChannels { get; set; } = [];
}
