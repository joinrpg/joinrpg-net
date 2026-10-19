using JoinRpg.DomainTypes.Notifications;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class NotificationLogModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int? UserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public NotificationChannel? Channel { get; set; }

    public void OnGet()
    {
    }
}
