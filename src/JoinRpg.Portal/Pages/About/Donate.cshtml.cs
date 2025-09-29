using JoinRpg.Portal.Menu;
using JoinRpg.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace JoinRpg.Portal.Pages.About;

[Authorize]
public class DonateModel(IOptions<DonateOptions> donateOptions, IClaimService claimService) : PageModel
{
    public async Task<ActionResult> OnGet()
    {
        var claimId = await claimService.SystemEnsureClaim(donateOptions.Value.DonateProject);
        return RedirectToAction("Edit", "Claim", new { projectId = claimId.ProjectId.Value, claimId = claimId.ClaimId });
    }
}
