namespace JoinRpg.Services.Notifications.Senders;

internal interface ISenderJob
{
    static abstract NotificationChannel Channel { get; }
    Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken);
}
/// <summary>
/// Результат отправки
/// </summary>
/// <param name="Succeeded"></param>
/// <param name="Repeatable">
/// Когда <see cref="Succeeded"/>=false, обозначает ошибку, которую есть смысл повторять
/// </param>
internal record struct SendingResult(bool Succeeded, bool Repeatable)
{
    public static SendingResult Success() => new(true, false);

    public static SendingResult Failure(bool repeatable) => new(false, repeatable);
}
