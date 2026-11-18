using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Interfaces;
using JoinRpg.Markdown;
using JoinRpg.Web.UserProfile;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.UserProfile;

[Authorize]
public class NotificationsModel(ICurrentUserAccessor currentUser, INotificationRepository notificationRepository) : PageModel
{
    private const int PageSize = 20;

    public async Task OnGet(int? before)
    {
        // На одно больше, чтобы понять, есть ли что показать по ссылке «Ранее».
        var lastNotifications = await notificationRepository.GetLastNotificationsForUser(
            currentUser.UserIdentification,
            NotificationChannel.ShowInUi,
            KeySetPagination.Descending(before, PageSize + 1));

        var page = lastNotifications.Take(PageSize).ToList();
        List = new UserNotificationListViewModel(
            [.. page.Select(x => new UserNotificationViewModel(x.Message.Header, x.Message.CreatedAt, x.Message.Body.ToHtmlString().Value))],
            NewestUrl: before is null ? null : Url.Page("/UserProfile/Notifications"),
            OlderUrl: lastNotifications.Count > PageSize ? Url.Page("/UserProfile/Notifications", new { before = page[^1].Id.Value }) : null);
    }

    internal UserNotificationListViewModel List { get; private set; } = null!;
}
