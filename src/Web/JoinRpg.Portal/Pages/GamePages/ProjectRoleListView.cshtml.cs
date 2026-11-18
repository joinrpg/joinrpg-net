using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

// Без [RequireMaster]: публичная сетка (PublicMode) доступна игроку/анониму.
// Доступ enforce'ится в сервисе ProjectRoleGridViewService по PublicMode.
public class ProjectRoleListViewModel : PageModel
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
