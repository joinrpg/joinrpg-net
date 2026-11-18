using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Web.AdminTools.Notifications;

public record NotificationChannelStatusViewModel(NotificationChannel Channel, int QueueLength, string? JobFullName, string? Error);
