namespace JoinRpg.Services.Notifications.Senders;

internal interface ISenderJob
{
    static abstract NotificationChannel Channel { get; }
    Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken);
    bool Enabled { get; }
}
