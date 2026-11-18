using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

public class DefaultSlotNotSetModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }
    public void OnGet()
    {
    }
}
