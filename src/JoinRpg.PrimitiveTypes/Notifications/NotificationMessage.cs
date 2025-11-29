using JoinRpg.DataModel;

namespace JoinRpg.PrimitiveTypes.Notifications;

/// Конкретный экземпляр уведомления для конкретного пользователя
public record NotificationMessageForRecipient(
    MarkdownString Body,
    UserIdentification Initiator,
    string Header,
    UserIdentification Recipient,
    IProjectEntityId? EntityReference,
    DateTimeOffset CreatedAt);

// Конкретный экземпляр уведомления для конкретного пользователя через конкретный канал
public record TargetedNotificationMessageForRecipient(NotificationMessageForRecipient Message, NotificationAddress NotificationAddress, int Attempts);

public record NotificationAddress
{
    public NotificationChannel Channel { get; }

    private object InternalValue { get; }

    public NotificationAddress(Email email)
    {
        InternalValue = email;
        Channel = NotificationChannel.Email;
    }

    public static NotificationAddress Ui() => new NotificationAddress(NotificationChannel.ShowInUi, "");

    public NotificationAddress(NotificationChannel notificationChannel, string channelSpeficValue)
    {
        Channel = notificationChannel;
        if (Channel == NotificationChannel.Email)
        {
            InternalValue = new Email(channelSpeficValue);
        }
        else if (Channel == NotificationChannel.ShowInUi)
        {
            InternalValue = "";
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
        return (Email)InternalValue!;
    }

    public void Deconstruct(out NotificationChannel channel, out string channelSpecificValue)
    {
        channel = Channel;
        channelSpecificValue = InternalValue.ToString() ?? "";
    }
}
