using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.DomainTypes.Notifications;

/// Конкретный экземпляр уведомления для конкретного пользователя
public record NotificationMessageForRecipient(
    MarkdownString Body,
    UserIdentification Initiator,
    string Header,
    UserIdentification Recipient,
    IProjectEntityId? EntityReference,
    DateTimeOffset CreatedAt,
    bool SkipSignature = false);

// Конкретный экземпляр уведомления для конкретного пользователя через конкретный канал
public record TargetedNotificationMessageForRecipient(NotificationMessageForRecipient Message, NotificationAddress NotificationAddress, int Attempts, NotificationId MessageId);

public record NotificationAddress
{
    public NotificationChannel Channel { get; }

    private object InternalValue { get; }

    public NotificationAddress(Email email)
    {
        InternalValue = email;
        Channel = NotificationChannel.Email;
    }

    public NotificationAddress(TelegramChatId id)
    {
        InternalValue = id;
        Channel = NotificationChannel.Telegram;
    }

    public static NotificationAddress Ui() => new NotificationAddress(NotificationChannel.ShowInUi, "");

    public NotificationAddress(NotificationChannel notificationChannel, string channelSpeficValue)
    {
        Channel = notificationChannel;
        if (Channel == NotificationChannel.Email)
        {
            InternalValue = Email.Parse(channelSpeficValue);
        }
        else if (Channel == NotificationChannel.Telegram)
        {
            InternalValue = ParseTelegramChatId(channelSpeficValue);
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

    private static TelegramChatId ParseTelegramChatId(string value)
    {
        if (TelegramChatId.TryParse(value, provider: null, out var chatId))
        {
            return chatId;
        }

        // До #4642 в очередь писали старый TelegramId: Telegram(<id>) или Telegram(<id>, @<name>).
        // Такие строки могли остаться в очереди, их тоже надо уметь прочитать (#5466).
        if (TelegramSocialLink.TryParse(value, provider: null, out var legacyLink) && legacyLink.ChatId is not null)
        {
            return legacyLink.ChatId;
        }

        throw new ArgumentException($"Could not parse Telegram address \"{value}\".", nameof(value));
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

    public TelegramChatId AsTelegram()
    {
        if (Channel != NotificationChannel.Telegram)
        {
            throw new InvalidOperationException();
        }
        return (TelegramChatId)InternalValue!;
    }
}
