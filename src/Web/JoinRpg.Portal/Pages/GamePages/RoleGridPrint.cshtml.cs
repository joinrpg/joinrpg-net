using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

// Без [RequireMaster]: доступ повторяет страницу сетки и проверяется в ProjectRoleGridViewService.
public class RoleGridPrintModel : PageModel
{
    public void OnGet()
    {
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public required int Id { get; set; }

    public ProjectRolesListIdentification RolesListId => new(ProjectId, Id);
}
