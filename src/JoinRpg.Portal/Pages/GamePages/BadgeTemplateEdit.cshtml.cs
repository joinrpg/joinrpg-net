using JoinRpg.DomainTypes.ProjectMetadata.Badges;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.Badges;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster(Permission.CanChangeProjectProperties)]
[ProjectShouldBeActive] // как и сервис шаблонов (MustBeActive): в заблокированном проекте сохранить нельзя
public class BadgeTemplateEditModel(IBadgeTemplateClient client) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? BadgeTemplateId { get; set; }

    public ProjectBadgeTemplateIdentification? TemplateId
        => BadgeTemplateId is int id ? new ProjectBadgeTemplateIdentification(ProjectId, id) : null;

    public EditBadgeTemplateViewModel? Model { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        if (TemplateId is { } templateId)
        {
            try
            {
                Model = await client.GetById(templateId);
            }
            catch (JoinRpgEntityNotFoundException)
            {
                return NotFound();
            }
        }
        else
        {
            Model = new EditBadgeTemplateViewModel();
        }
        return Page();
    }
}
