namespace JoinRpg.Services.Interfaces.Notification;

public interface ITelegramNotificationService
{
    Task<SendingResult> SendTelegramNotification(TelegramChatId chatId, TelegramHtmlString contents);
    Task<string?> GetMyUserName(CancellationToken cancellationToken);
}
