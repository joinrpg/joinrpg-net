using JoinRpg.DomainTypes.Notifications;

namespace JoinRpg.Web.AdminTools.Notifications;

/// <summary>
/// Адрес страницы лога уведомлений в админке с фильтрами.
/// </summary>
public static class NotificationLogUrl
{
    /// <param name="before">Постранично: только уведомления с id меньше этого. null — с самых новых.</param>
    public static string For(UserIdentification? recipient = null, NotificationChannel? channel = null, int? before = null)
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
        if (before is not null)
        {
            query.Add($"before={before.Value}");
        }
        return query.Count == 0 ? "/admin/notifications/log" : "/admin/notifications/log?" + string.Join("&", query);
    }
}
