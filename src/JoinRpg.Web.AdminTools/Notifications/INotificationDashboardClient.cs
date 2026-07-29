namespace JoinRpg.Web.AdminTools.Notifications;

public interface INotificationDashboardClient
{
    Task<NotificationChannelStatusViewModel[]> GetStatus();
}
