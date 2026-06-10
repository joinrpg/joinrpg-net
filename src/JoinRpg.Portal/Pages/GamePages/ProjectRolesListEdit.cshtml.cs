using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectMasterTools.ProjectRolesLists;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.GamePages;

[RequireMaster(Permission.CanEditRoles)]
public class ProjectRolesListEditModel : PageModel
{
    private readonly IProjectRolesListClient _client;

    public ProjectRolesListEditModel(IProjectRolesListClient client)
    {
        _client = client;
    }

    [BindProperty(SupportsGet = true)]
    public required ProjectIdentification ProjectId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? Id { get; set; }

    public bool IsEditMode => Id.HasValue;

    public EditProjectRolesListViewModel? Model { get; private set; }

    public async Task<IActionResult> OnGet()
    {
        if (IsEditMode)
        {
            try
            {
                var domain = await _client.GetById(new ProjectRolesListIdentification(ProjectId, Id.Value));
                Model = EditProjectRolesListViewModel.FromDomain(domain);
            }
            catch
            {
                return NotFound();
            }
        }
        else
        {
            Model = new EditProjectRolesListViewModel();
        }
        return Page();
    }
}
