using JoinRpg.Data.Interfaces;
using JoinRpg.Web.AdminTools;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

public class AdminHotRolesListModel(IHotCharactersRepository hotCharactersRepository) : PageModel
{
    public async Task OnGetAsync()
    {
        HotRoles = [..
            (await hotCharactersRepository.GetHotCharactersFromAllProjects())
            .Select(c => new AdminHotRoleViewModel(c))];
    }

    public IReadOnlyCollection<AdminHotRoleViewModel> HotRoles { get; set; } = null!;
}
