using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/claim-list/[action]")]
public class ClaimListController(IClaimListClient claimClient, IClaimGridClient claimGridClient) : ControllerBase
{
    [HttpGet]
    [MasterAuthorize]
    public async Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims([FromQuery] ProjectIdentification projectId, [FromQuery] ClaimStatusSpec claimStatusSpec)
    {
        return await claimClient.GetClaims(projectId, claimStatusSpec);
    }

    [HttpGet]
    [Authorize]
    public async Task<IReadOnlyCollection<ClaimListItemForCaptainViewModel>> GetForCaptain([FromQuery] ProjectIdentification projectId, [FromQuery] ClaimStatusSpec claimStatusSpec)
    {
        return await claimGridClient.GetForCaptain(projectId, claimStatusSpec);
    }
}
