using JoinRpg.Interfaces;

namespace JoinRpg.Services.Interfaces.Notification;

public interface ITelegramNotificationService
{
    Task SendTelegramNotification(TelegramId telegramId, TelegramHtmlString contents);
}
