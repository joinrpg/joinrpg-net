using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.Notifications;

/// Конкретный экземпляр уведомления для конкретного пользователя
public record NotificationMessageForRecipient(MarkdownString Body, UserIdentification Initiator, string Header, UserIdentification Recipient);

// Конкретный экземпляр уведомления для конкретного пользователя через конкретный канал
public record TargetedNotificationMessageForRecipient(MarkdownString Body,
                                          UserIdentification Initiator,
                                          string Header,
                                          UserIdentification Recipient,
                                          NotificationAddress NotificationAddress)
    : NotificationMessageForRecipient(Body, Initiator, Header, Recipient);

public record NotificationAddress
{
    public NotificationChannel Channel { get; }

    private object InternalValue { get; }

    public NotificationAddress(Email email)
    {
        InternalValue = email;
        Channel = NotificationChannel.Email;
    }

    public NotificationAddress(NotificationChannel notificationChannel, string channelSpeficValue)
    {
        Channel = notificationChannel;
        if (Channel == NotificationChannel.Email)
        {
            InternalValue = new Email(channelSpeficValue);
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(notificationChannel));
        }
    }

    public Email AsEmail()
    {
        if (Channel != NotificationChannel.Email)
        {
            throw new InvalidOperationException();
        }
        return (Email)InternalValue;
    }

    public void Deconstruct(out NotificationChannel channel, out string channelSpecificValue)
    {
        channel = Channel;
        channelSpecificValue = InternalValue.ToString() ?? "";
    }
}
