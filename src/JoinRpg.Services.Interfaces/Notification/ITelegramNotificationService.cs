using JoinRpg.Common.PrimitiveTypes;

namespace JoinRpg.Services.Interfaces.Notification;

public interface ITelegramNotificationService
{
    Task<SendingResult> SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents);
    Task<string?> GetMyUserName(CancellationToken cancellationToken);
}
