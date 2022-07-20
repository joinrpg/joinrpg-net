using System.ComponentModel.DataAnnotations;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces.Characters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster]
public class ConvertToSlotModel : PageModel
{
    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPost([FromServices] ICharacterService characterService)
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var characterId = await characterService.CreateSlotFromGroup(ProjectId, CharacterGroupId, SlotName);

        return RedirectToAction("Details", "Character", new { ProjectId, characterId });
    }

    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int CharacterGroupId { get; set; }

    [BindProperty]
    [Required]
    [Display(Name = "Название слота")]
    public string SlotName { get; set; }
}
