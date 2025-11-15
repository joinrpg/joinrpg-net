using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.Claims.UnifiedGrid;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/claim-list/[action]")]
public class ClaimListController(IClaimListClient claimClient, IUnifiedGridClient claimGridClient) : ControllerBase
{
    [HttpGet]
    [MasterAuthorize]
    public async Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims([FromQuery] ProjectIdentification projectId, [FromQuery] ClaimStatusSpec claimStatusSpec)
    {
        return await claimClient.GetClaims(projectId, claimStatusSpec);
    }

    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyCollection<UgItemForCaptainViewModel>> GetForCaptain([FromQuery] ProjectIdentification projectId, [FromQuery] UgStatusFilterView filter)
    {
        return await claimGridClient.GetForCaptain(projectId, filter);
    }
}
