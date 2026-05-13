using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster(Permission.CanManageClaims)]
public class InvitePlayerModel(IProjectMetadataRepository projectMetadataRepository) : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(ProjectId);
        if (!projectInfo.IsActive)
        {
            throw new ProjectDeactivatedException(ProjectId);
        }
        return Page();
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CharacterId { get; set; }
}
