using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Data.Write.Interfaces;
public interface INotificationRepository
{
    Task InsertNotifications(NotificationMessageDto[] notifications);
    Task<(NotificationId Id, NotificationMessageDto Message)> SelectNextNotificationForSending(NotificationChannel channel);
    Task MarkSendSuccess(NotificationId id, NotificationChannel channel);
    Task MarkSendFailed(NotificationId id, NotificationChannel channel);
}

public class NotificationMessageDto
{
    public required MarkdownString Body { get; set; }
    public required UserIdentification Initiator { get; set; }

    public required Email InitiatorAddress { get; set; }
    public required string Header { get; set; }

    public required UserIdentification Recepient { get; set; }

    public required NotificationChannelDto[] Channels { get; set; }
}

public record NotificationChannelDto(NotificationChannel Channel, string ChannelSpecificValue)
{
    public Email GetEmail()
    {
        if (Channel != NotificationChannel.Email)
        {
            throw new InvalidOperationException();
        }
        return new Email(ChannelSpecificValue);
    }

    public long GetTelegramId()
    {
        if (Channel != NotificationChannel.Telegram)
        {
            throw new InvalidOperationException();
        }
        return long.Parse(ChannelSpecificValue); ;
    }
}
