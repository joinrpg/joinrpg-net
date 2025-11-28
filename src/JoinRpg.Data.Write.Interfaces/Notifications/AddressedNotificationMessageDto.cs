using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Data.Write.Interfaces.Notifications;

public class AddressedNotificationMessageDto : NotificationMessageBaseDto
{
    public required NotificationId Id { get; init; }
    public required NotificationChannelDto Channel { get; init; }
}
