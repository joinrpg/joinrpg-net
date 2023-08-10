using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;


namespace JoinRpg.Portal.Pages.GamePages;

[RequireMasterOrAdmin(Web.Models.Permission.CanEditRoles)]
public class MassConvertToSlotModel : PageModel
{
    private readonly IProjectRepository projectRepository;

    public MassConvertToSlotModel(IProjectRepository projectRepository)
    {
        this.projectRepository = projectRepository;
    }
    public async Task OnGet()
    {
        var project = await projectRepository.GetProjectAsync(ProjectId);
        GroupsToChange = project.CharacterGroups
            .Where(cg => !cg.IsSpecial && (cg.HaveDirectSlots || cg.Claims.Any()))
            .Select(cg => new CharacterGroupLinkSlimViewModel(new(cg.ProjectId), cg.CharacterGroupId, cg.CharacterGroupName, cg.IsPublic, cg.IsActive))
            .ToList();
    }


    [BindProperty(SupportsGet = true)]
    public int ProjectId { get; set; }

    public List<CharacterGroupLinkSlimViewModel> GroupsToChange { get; set; }
}
