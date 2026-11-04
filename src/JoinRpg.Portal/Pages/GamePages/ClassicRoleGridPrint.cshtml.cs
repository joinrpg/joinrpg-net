using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

// Без [RequireMaster]: классическая сетка публична, доступ к сохранённой проверяет сервис.
public class ClassicRoleGridPrintModel : PageModel
{
    public void OnGet()
    {
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CharacterGroupId { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool HotOnly { get; set; }

    public CharacterGroupIdentification? ClassicGroupId => CharacterGroupIdentification.FromOptional(ProjectId, CharacterGroupId);
}
