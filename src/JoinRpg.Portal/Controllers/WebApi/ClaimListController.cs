using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Web.ProjectCommon.Claims;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.WebApi;
[Route("/webapi/ClaimOperations/[action]")]
[MasterAuthorize]
public class ClaimListController(IClaimListClient claimClient) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyCollection<ClaimLinkViewModel>> GetClaims([FromQuery] ProjectIdentification projectId, [FromQuery] ClaimStatusSpec claimStatusSpec)
    {
        return await claimClient.GetClaims(projectId, claimStatusSpec);
    }
}
