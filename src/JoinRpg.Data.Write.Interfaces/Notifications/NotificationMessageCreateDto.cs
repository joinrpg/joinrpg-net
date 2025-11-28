namespace JoinRpg.Data.Write.Interfaces.Notifications;

public class NotificationMessageCreateDto : NotificationMessageBaseDto
{
    public required NotificationChannelDto[] Channels { get; init; }
}
