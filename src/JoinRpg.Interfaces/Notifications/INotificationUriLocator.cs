namespace JoinRpg.Interfaces.Notifications;

public interface INotificationUriLocator<T>
{
    Uri GetUri(T target);
}
