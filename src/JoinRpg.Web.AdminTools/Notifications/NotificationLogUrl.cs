using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Web.AdminTools.Notifications;

/// <summary>
/// Адрес страницы лога уведомлений в админке с фильтрами.
/// </summary>
public static class NotificationLogUrl
{
    public static string For(UserIdentification? recipient = null, NotificationChannel? channel = null)
    {
        var query = new List<string>();
        if (recipient is not null)
        {
            query.Add($"userId={recipient.Value}");
        }
        if (channel is not null)
        {
            query.Add($"channel={channel}");
        }
        return query.Count == 0 ? "/admin/notifications/log" : "/admin/notifications/log?" + string.Join("&", query);
    }
}
