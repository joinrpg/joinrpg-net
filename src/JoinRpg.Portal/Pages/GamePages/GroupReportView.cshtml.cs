using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models.CharacterGroups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster]
public class GroupReportPageModel(
    IProjectMetadataRepository projectMetadataRepository,
    ICharacterGroupRepository charGroupRepository,
    ICurrentUserAccessor currentUserAccessor) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CharacterGroupId { get; set; }

    public CharacterGroupDetailsViewModel Details { get; private set; } = null!;

    public CharacterGroupIdentification GroupId { get; private set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        GroupId = new CharacterGroupIdentification(new ProjectIdentification(ProjectId), CharacterGroupId!.Value);

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(GroupId.ProjectId);
        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(GroupId);
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        Details = new CharacterGroupDetailsViewModel(charGroupFullInfo, projectInfo, currentUserAccessor.UserIdentificationOrDefault, GroupNavigationPage.Report);

        return Page();
    }
}
