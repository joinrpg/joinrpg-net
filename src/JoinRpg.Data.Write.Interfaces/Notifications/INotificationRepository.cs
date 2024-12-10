using JoinRpg.PrimitiveTypes.Notifications;

namespace JoinRpg.Data.Write.Interfaces.Notifications;
public interface INotificationRepository
{
    Task InsertNotifications(NotificationMessageDto[] notifications);
    Task<(NotificationId Id, NotificationMessageDto Message)?> SelectNextNotificationForSending(NotificationChannel channel);
    Task MarkSendSuccess(NotificationId id, NotificationChannel channel);
    Task MarkSendFailed(NotificationId id, NotificationChannel channel);
}
