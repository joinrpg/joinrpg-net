namespace JoinRpg.Services.Interfaces.Notification;

public interface IClaimNotificationService
{
    Task SendNotification(ClaimSimpleChangedNotification model);
}
