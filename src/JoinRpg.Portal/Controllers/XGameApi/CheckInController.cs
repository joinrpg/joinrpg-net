using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Domain;
using JoinRpg.Portal.Infrastructure.Authorization;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.XGameApi.Contract;
using Microsoft.AspNetCore.Mvc;

namespace JoinRpg.Web.Controllers.XGameApi
{
    [Route("x-game-api/{projectId}/checkin"), XGameMasterAuthorize()]
    public class CheckInController : XGameApiController
    {
        private IClaimsRepository ClaimsRepository { get; }

        private IClaimService ClaimsService { get; }


        public CheckInController(IProjectRepository projectRepository,
            IClaimsRepository claimsRepository,
            IClaimService claimsService) : base(projectRepository)
        {
            ClaimsRepository = claimsRepository;
            ClaimsService = claimsService;
        }

        /// <summary>
        /// Claims that are ready for checkin
        /// </summary>
        [Route("allclaims")]
        [HttpGet]
        public async Task<IEnumerable<ClaimHeaderInfo>> GetClaimsForCheckIn(int projectId)
        {
            return (await ClaimsRepository.GetClaimHeadersWithPlayer(projectId,
                    ClaimStatusSpec.ReadyForCheckIn))
                .Select(claim =>

                    new ClaimHeaderInfo
                    {
                        ClaimId = claim.ClaimId,
                        CharacterName = claim.CharacterName,
                        Player = new PlayerInfo()
                        {
                            PlayerId = claim.Player.UserId,
                            NickName = claim.Player.GetDisplayName(),
                            FullName = claim.Player.FullName,
                            OtherNicks = claim.Player.Extra?.Nicknames ?? "",
                        },
                    });
        }


        [Route("stat")]
        [HttpGet]
        public async Task<CheckInStats> GetCheckInStat(int projectId)
        {
            return new CheckInStats()
            {
                CheckIn = (await ClaimsRepository.GetClaimHeadersWithPlayer(projectId,
                    ClaimStatusSpec.CheckedIn)).Count,
                Ready = (await ClaimsRepository.GetClaimHeadersWithPlayer(projectId,
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
        public async Task<ActionResult<ClaimCheckInValidationResult>> PrepareClaimFoCheckIn([FromQuery]
            int projectId,
            [FromQuery]
            int claimId)
        {
            var claim = await ClaimsRepository.GetClaim(projectId, claimId);
            if (claim == null)
            {
                return NotFound();
            }

            var validator = new ClaimCheckInValidator(claim);
            return
                new ClaimCheckInValidationResult
                {
                    ClaimId = claim.ClaimId,
                    CheckedIn = !validator.NotCheckedInAlready,
                    Approved = validator.IsApproved,
                    CheckInPossible = validator.CanCheckInInPrinciple,
                    EverythingFilled = !validator.NotFilledFields.Any(),
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
        public async Task<ActionResult<string>> CheckinClaim([FromQuery]
            int projectId,
            [FromBody]
            CheckInCommand command)
        {
            var claim = await ClaimsRepository.GetClaim(projectId, command.ClaimId);
            if (claim == null)
            {
                return NotFound();
            }

            await ClaimsService.CheckInClaim(projectId, command.ClaimId, command.MoneyPaid);
            return "OK";
        }
    }
}
