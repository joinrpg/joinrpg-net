namespace JoinRpg.Web.UserProfile;

/// <summary>
/// Страница уведомлений пользователя: новые сверху.
/// </summary>
/// <param name="NewestUrl">Ссылка «К последним»; null — это и есть первая страница.</param>
/// <param name="OlderUrl">Ссылка «Ранее»; null — более старых уведомлений нет.</param>
public record UserNotificationListViewModel(
    IReadOnlyCollection<UserNotificationViewModel> Items,
    string? NewestUrl,
    string? OlderUrl);

public record UserNotificationViewModel(string Header, DateTimeOffset Time, string BodyHtml);
