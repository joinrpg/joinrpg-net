using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

/// <summary>Шлюз к API ПСКБ: список платежей и параметры платежа (ADR025).</summary>
[AdminAuthorize]
public class PscbPaymentsModel : PageModel
{
    public void OnGet()
    {
    }
}
