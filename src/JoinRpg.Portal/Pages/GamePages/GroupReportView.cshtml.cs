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
    public ProjectIdentification ProjectId { get; set; } = null!;

    [BindProperty(SupportsGet = true)]
    public CharacterGroupIdentification? CharacterGroupId { get; set; }

    public CharacterGroupDetailsViewModel Details { get; private set; } = null!;

    public CharacterGroupIdentification GroupId { get; private set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(ProjectId);

        // Страница "/roles/all/report" не передаёт characterGroupId — в этом случае берём корневую группу проекта.
        GroupId = CharacterGroupId ?? projectInfo.RootCharacterGroupId;

        var charGroupFullInfo = await charGroupRepository.GetCharacterGroupFullInfo(GroupId);
        if (charGroupFullInfo is null)
        {
            return NotFound();
        }

        Details = new CharacterGroupDetailsViewModel(charGroupFullInfo, projectInfo, currentUserAccessor.UserIdentificationOrDefault, GroupNavigationPage.Report);

        return Page();
    }
}
