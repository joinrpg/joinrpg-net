namespace JoinRpg.Services.Notifications.Senders;

public interface ISenderJob
{
    // static virtual (не abstract) с дефолтной реализацией: static abstract-член без реализации не
    // позволяет использовать сам интерфейс ISenderJob как тип (нужно для IEnumerable<ISenderJob> из DI) — CS8920.
    static virtual NotificationChannel Channel => throw new NotSupportedException();

    /// <summary>
    /// Инстанс-версия <see cref="Channel"/> — нужна, чтобы перечислить зарегистрированные джобы через DI
    /// (static-член нельзя вызвать через ISenderJob-ссылку).
    /// </summary>
    NotificationChannel InstanceChannel { get; }

    Task<SendingResult> SendAsync(TargetedNotificationMessageForRecipient message, CancellationToken stoppingToken);
    bool Enabled { get; }
}
