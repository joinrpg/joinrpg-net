using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.WebPortal.Managers.Characters;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/claims"), XGameMasterAuthorize()]
public class ClaimsApiController(IClaimInfoRepository claimInfoRepository) : XGameApiController
{
    [HttpGet]
    [Route("{claimId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<ClaimDetails>> GetOne(int projectId, int claimId)
    {
        // Доменный агрегат заявки вместе с профилем игрока (ADR021): контакты с EF-сущности
        // User.Extra догружались лениво (#4965).
        var claimInfo = await claimInfoRepository.GetClaimInfoOrDefault(new ClaimIdentification(projectId, claimId));
        if (claimInfo is null)
        {
            return NotFound();
        }
        return new ClaimDetails(
            claimInfo.ClaimId.ClaimId,
            claimInfo.Character.Id.CharacterId,
            ApiInfoBuilder.ToPlayerContacts(claimInfo.Player),
            ApiInfoBuilder.ToApiStatus(claimInfo.Claim.Status));
    }
}
