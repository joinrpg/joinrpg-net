using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Interfaces.Notifications;
public interface INotificationService
{
    /// <summary>
    /// Независимо от настроек, послать через определенный канал
    /// </summary>
    Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel);
    Task QueueNotification(NotificationEvent notificationMessage);
}
