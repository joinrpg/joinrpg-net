using JoinRpg.Services.Interfaces.Notification;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

public class TestMessageModel : PageModel
{
    public void OnGet()
    {
    }

    public async Task OnPostAsync([FromServices] IAdminNotificationService adminNotificationService)
    {
        await adminNotificationService.SendTestMessage();
    }
}
