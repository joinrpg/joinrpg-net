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

    public Task QueueDirectNotification(NotificationEvent notificationMessage, NotificationChannel directChannel)
    {
        Queued.Add(notificationMessage);
        return Task.CompletedTask;
    }
}
