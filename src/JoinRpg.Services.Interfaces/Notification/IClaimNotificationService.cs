namespace JoinRpg.Services.Interfaces.Notification;

public interface IClaimNotificationService
{
    Task SendNotification(ClaimSimpleChangedNotification model);
    Task SendNotification(ClaimOnlinePaymentNotification model);
}

public interface IForumNotificationService
{
    Task SendNotification(ForumMessageNotification model);
}
