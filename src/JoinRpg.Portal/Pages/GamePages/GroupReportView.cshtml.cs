using JoinRpg.Data.Interfaces;
using JoinRpg.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.Models.CharacterGroups;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster]
public class GroupReportPageModel(IProjectRepository projectRepository, ICurrentUserAccessor currentUserAccessor) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? CharacterGroupId { get; set; }

    public CharacterGroupDetailsViewModel Details { get; private set; } = null!;

    public CharacterGroupIdentification GroupId { get; private set; } = null!;

    public async Task<IActionResult> OnGet()
    {
        var field = await projectRepository.LoadGroupWithTreeAsync(ProjectId, CharacterGroupId);
        if (field is null)
        {
            return NotFound();
        }

        Details = new CharacterGroupDetailsViewModel(field, currentUserAccessor.UserIdOrDefault, GroupNavigationPage.Report);
        GroupId = new CharacterGroupIdentification(new ProjectIdentification(ProjectId), field.CharacterGroupId);
        return Page();
    }
}
