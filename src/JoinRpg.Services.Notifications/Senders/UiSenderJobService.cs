namespace JoinRpg.Services.Notifications.Senders;
internal class UiSenderJobService : ISenderJob
{
    public static NotificationChannel Channel => NotificationChannel.ShowInUi;

    public bool Enabled => true;

    public Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken)
    {
        // Это просто то, что показывается в UI. Отправлять не надо.
        // Здесь для отладки отправлялки и чтобы сообщения в этом канале не зависали в статусе в очереди.
        return Task.FromResult(SendingResult.Success());
    }
}
