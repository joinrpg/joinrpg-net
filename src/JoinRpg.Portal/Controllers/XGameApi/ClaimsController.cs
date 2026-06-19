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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<ClaimInfo>> GetOne(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaimWithDetails(new ClaimIdentification(projectId, claimId));
        if (claim is null)
        {
            return NotFound();
        }
        return new ClaimInfo(claim.ClaimId, claim.CharacterId, ApiInfoBuilder.ToPlayerContacts(claim.Player), (ClaimStatusEnum)claim.ClaimStatus);
    }
}
