using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Interfaces.Notifications;
public interface INotificationService
{
    Task QueueNotification(NotificationEvent notificationMessage);
}
