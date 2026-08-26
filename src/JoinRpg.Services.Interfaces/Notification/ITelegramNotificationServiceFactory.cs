namespace JoinRpg.Services.Interfaces.Notification;

/// <summary>
/// Резолвит клиента отправки в Telegram по ключу бота.
/// Точка расширения под будущий мультибот (ADR010 §5) — сегодня поддерживается только
/// дефолтный бот приложения.
/// </summary>
public interface ITelegramNotificationServiceFactory
{
    /// <param name="botKey">null/пусто — дефолтный бот приложения.</param>
    ITelegramNotificationService GetService(string? botKey);
}
