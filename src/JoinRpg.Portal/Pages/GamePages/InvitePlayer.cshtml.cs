using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster(Permission.CanManageClaims)]
public class InvitePlayerModel : PageModel
{
    public void OnGet()
    {
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }
}