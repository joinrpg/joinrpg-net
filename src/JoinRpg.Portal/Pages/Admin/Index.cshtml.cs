using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
