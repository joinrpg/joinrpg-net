using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Domain.Problems;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.Services.Interfaces;
using JoinRpg.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Portal.Controllers.XGameApi;

[Route("x-game-api/{projectId}/checkin"), XGameMasterAuthorize()]
public class CheckInController(
    IClaimsRepository claimsRepository,
    IClaimService claimsService,
    IProblemValidator<Claim> claimValidator,
    IProjectMetadataRepository projectMetadataRepository) : XGameApiController
{

    /// <summary>
    /// Claims that are ready for checkin
    /// </summary>
    [Route("allclaims")]
    [HttpGet]
    public async Task<IEnumerable<ClaimHeaderInfo>> GetClaimsForCheckIn(int projectId)
    {
        return (await claimsRepository.GetClaimHeadersWithPlayer(projectId,
                ClaimStatusSpec.ReadyForCheckIn))
            .Select(claim =>

                new ClaimHeaderInfo
                {
                    ClaimId = claim.ClaimId.ClaimId,
                    CharacterName = claim.CharacterName,
                    Player = new CheckinPlayerInfo()
                    {
                        PlayerId = claim.Player.UserId,
                        NickName = claim.Player.DisplayName.DisplayName,
                        FullName = claim.Player.DisplayName.FullName,
                        OtherNicks = claim.ExtraNicknames ?? "",
                    },
                });
    }


    [Route("stat")]
    [HttpGet]
    public async Task<CheckInStats> GetCheckInStat(int projectId)
    {
        return new CheckInStats()
        {
            CheckIn = (await claimsRepository.GetClaimHeadersWithPlayer(projectId,
                ClaimStatusSpec.CheckedIn)).Count,
            Ready = (await claimsRepository.GetClaimHeadersWithPlayer(projectId,
                ClaimStatusSpec.ReadyForCheckIn)).Count,

        };
    }

    /// <summary>
    /// Stub method. Not tested.
    /// </summary>
    [Route("{claimId}/prepare")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<ClaimCheckInValidationResult>> PrepareClaimFoCheckIn([FromRoute]
        int projectId,
        [FromRoute]
        int claimId)
    {
        var claim = await claimsRepository.GetClaim(projectId, claimId);
        if (claim == null)
        {
            return NotFound();
        }

        var projectInfo = await projectMetadataRepository.GetProjectMetadata(new(projectId));

        var validator = new ClaimCheckInValidator(claim, claimValidator, projectInfo);
        return
            new ClaimCheckInValidationResult
            {
                ClaimId = claim.ClaimId,
                CheckedIn = !validator.NotCheckedInAlready,
                Approved = validator.IsApproved,
                CheckInPossible = validator.CanCheckInInPrinciple,
                EverythingFilled = !validator.FieldProblems.Any(),
                ClaimFeeBalance = validator.FeeDue,
                Handouts = new[] //TODO FIX ME
                {
                    new HandoutItem {Label = "Хайратник"},
                    new HandoutItem {Label = "Ленточка"},
                },
            };
    }

    /// <summary>
    /// Stub method. Not tested.
    /// </summary>
    [Route("checkin")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesDefaultResponseType]
    public async Task<ActionResult<string>> CheckinClaim([FromRoute]
        int projectId,
        [FromBody]
        CheckInCommand command)
    {
        var claim = await claimsRepository.GetClaim(projectId, command.ClaimId);
        if (claim == null)
        {
            return NotFound();
        }

        await claimsService.CheckInClaim(claim.GetId(), command.MoneyPaid);
        return "OK";
    }
}
