using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.About;

public class BudgetModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/About/Index", null, null, "budget");
}
