using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Notification;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class TestMessageModel(IAdminNotificationService adminNotificationService) : PageModel
{
    public void OnGet() { }

    public async Task OnPostAsync()
    {
        await adminNotificationService.SendTestMessage();
    }
}
