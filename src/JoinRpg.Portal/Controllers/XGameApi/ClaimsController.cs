using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/claims"), XGameMasterAuthorize()]
public class ClaimsApiController(IClaimsRepository claimsRepository) : XGameApiController
{
    [HttpGet]
    [Route("{claimId}")]
    public async Task<ClaimInfo> GetOne(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaimWithDetails(projectId, claimId);
        return new ClaimInfo(claim.ClaimId, claim.CharacterId, ApiInfoBuilder.ToPlayerContacts(claim.Player), (ClaimStatusEnum)claim.ClaimStatus);
    }
}
