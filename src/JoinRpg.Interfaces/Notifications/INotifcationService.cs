namespace JoinRpg.Interfaces.Notifications;
public interface INotifcationService
{
    Task QueueNotification(NotificationMessage notificationMessage);
}
