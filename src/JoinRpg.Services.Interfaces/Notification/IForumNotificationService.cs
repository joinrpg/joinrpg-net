namespace JoinRpg.Services.Interfaces.Notification;

public interface IForumNotificationService
{
    Task SendNotification(ForumMessageNotification model);
}
