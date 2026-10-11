using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces.Notifications;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>Записывает поставленные в очередь уведомления для проверки в тестах.</summary>
internal sealed class FakeNotificationService : INotificationService
{
    public List<NotificationEvent> Queued { get; } = [];

    public Task QueueNotification(NotificationEvent notificationMessage)
    {
        Queued.Add(notificationMessage);
        return Task.CompletedTask;
    }

    /// <summary>Каналы прямых уведомлений — в том же порядке, что и <see cref="Queued"/> для них.</summary>
    public List<NotificationChannel> DirectChannels { get; } = [];

    public Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
    {
        Queued.Add(notificationMessage);
        DirectChannels.Add(directChannel);
        return Task.CompletedTask;
    }
}
