using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

/// <summary>Админское слияние аккаунтов (ADR024).</summary>
[AdminAuthorize]
public class UserMergeModel : PageModel
{
    public void OnGet()
    {
    }
}
