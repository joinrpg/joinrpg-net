using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/claims"), XGameMasterAuthorize()]
public class ClaimsApiController(IClaimsRepository claimsRepository, IUserRepository userRepository) : XGameApiController
{
    [HttpGet]
    [Route("{claimId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<ClaimInfo>> GetOne(int projectId, int claimId)
    {
        var claim = await claimsRepository.GetClaim(new ClaimIdentification(projectId, claimId));
        if (claim is null)
        {
            return NotFound();
        }
        // Контакты — из UserInfo, как в API персонажей: User.Extra с сущности догружался бы лениво (#4965).
        var player = await userRepository.GetRequiredUserInfo(new UserIdentification(claim.PlayerUserId));
        return new ClaimInfo(claim.ClaimId, claim.CharacterId, ApiInfoBuilder.ToPlayerContacts(player), (ClaimStatusEnum)claim.ClaimStatus);
    }
}
