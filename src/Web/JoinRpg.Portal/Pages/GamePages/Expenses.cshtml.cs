using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster]
public class ExpensesModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }
}
