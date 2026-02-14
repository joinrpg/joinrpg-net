using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Web.AdminTools.KogdaIgra;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JoinRpg.Portal.Pages.Admin;

[AdminAuthorize]
public class SyncWithKogdaIgraModel(IKogdaIgraSyncClient kogdaIgraSyncClient) : PageModel
{
    public SyncStatusViewModel KogdaIgraSyncStatus { get; set; } = null!;
    public async Task OnGet()
    {
        KogdaIgraSyncStatus = await kogdaIgraSyncClient.GetSyncStatus();
    }
}
