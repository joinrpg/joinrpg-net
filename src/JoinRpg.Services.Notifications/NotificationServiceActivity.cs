using System.Diagnostics;

namespace JoinRpg.Services.Notifications;

public static class NotificationServiceActivity
{
    public const string ActivitySourceName = nameof(NotificationServiceImpl);
    public static readonly ActivitySource ActivitySource = new(ActivitySourceName);
}
