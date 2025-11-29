using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.PrimitiveTypes.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.UserProfile;

[Authorize]
public class NotificationsModel(ICurrentUserAccessor currentUser, INotificationRepository notificationRepository) : PageModel
{
    internal record NotificationViewModel(string Header, DateTimeOffset Time, MarkupString Body);
    public async Task OnGet()
    {
        var lastNotifications = await notificationRepository.GetLastNotificationsForUser(currentUser.UserIdentification, NotificationChannel.ShowInUi, new KeySetPagination(null, 20));
        Notifications = [.. lastNotifications.Select(x => new NotificationViewModel(x.Message.Header, x.Message.CreatedAt, x.Message.Body.ToHtmlString()))];
    }

    internal IReadOnlyCollection<NotificationViewModel> Notifications { get; set; } = null!;
}
