using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Data.Write.Interfaces.Notifications;

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
