namespace JoinRpg.Services.Notifications.Senders;

internal interface ISenderJob
{
    static abstract NotificationChannel Channel { get; }
    Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken);
    bool Enabled { get; }
}
/// <summary>
/// Результат отправки
/// </summary>
/// <param name="Succeeded"></param>
/// <param name="Repeatable">
/// Когда <see cref="Succeeded"/>=false, обозначает ошибку, которую есть смысл 
/// </param>
/// <param name="Common">
/// Проблема связана не с конкретным сообщением, а с общим статусом сервиса (нужно произвести кулдаун, но не увеличивать счетчик попыток для отправки)
/// </param>
internal record struct SendingResult(bool Succeeded, bool Repeatable, bool Common)
{
    public static SendingResult Success() => new(true, false, false);

    public static SendingResult RepeatableFailure() => new(false, true, false);
    public static SendingResult CommonFailure() => new(false, true, true);
}
