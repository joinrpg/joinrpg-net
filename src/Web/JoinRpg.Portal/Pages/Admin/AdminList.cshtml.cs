using JoinRpg.Common.WebComponents;
using JoinRpg.Data.Interfaces;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.ProjectCommon;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class AdminListModel(IUserRepository userRepository) : PageModel
{
    public async Task OnGetAsync()
    {
        var admins = await userRepository.GetAdminUserInfoHeaders();
        Admins = [.. admins
            .OrderBy(a => a.DisplayName.DisplayName)
            .Select(a => a.ToUserLinkViewModel())];
    }

    public IReadOnlyCollection<UserLinkViewModel> Admins { get; set; } = null!;
}
