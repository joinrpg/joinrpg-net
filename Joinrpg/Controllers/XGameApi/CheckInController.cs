using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.XGameApi.Contract;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-game-api/{projectId}/checkin"), XGameAuthorize()]
  public class CheckInController : XGameApiController
  {
    [ProvidesContext]
    private IClaimsRepository ClaimsRepository { get; }

    [ProvidesContext]
    private IClaimService ClaimsService { get; }


    public CheckInController(IProjectRepository projectRepository,
      IClaimsRepository claimsRepository, IClaimService claimsService) : base(projectRepository)
    {
      ClaimsRepository = claimsRepository;
      ClaimsService = claimsService;
    }

    [Route("allclaims")]
    public async Task<IEnumerable<ClaimHeaderInfo>> GetClaimsForCheckIn(int projectId)
    {
      return (await ClaimsRepository.GetClaimHeadersWithPlayer(projectId, ClaimStatusSpec.ReadyForCheckIn)).Select(claim =>

        new ClaimHeaderInfo
        {
          ClaimId = claim.ClaimId,
          CharacterName = claim.CharacterName,
          Player = new PlayerInfo()
          {
            PlayerId = claim.Player.UserId,
            NickName = claim.Player.DisplayName,
            FullName = claim.Player.FullName,
            OtherNicks = claim.Player.Extra?.Nicknames ?? ""
          },
        });
    }


    [Route("stat")]
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
    [Route("{claimId}/prepare")]
    [HttpGet]
    public async Task<ClaimCheckInValidationResult> PrepareClaimFoCheckIn([FromUri] int projectId,
      [FromUri] int claimId)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, claimId);
      if (claim == null)
      {
        throw new HttpResponseException(HttpStatusCode.NotFound);
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
          Handouts =  new[] //TODO FIX ME
          {
            new HandoutItem {Label = "Хайратник"},
            new HandoutItem {Label = "Ленточка"}
          }
        };
    }

    [Route("checkin")]
    [HttpPost]
    public async Task<string> CheckinClaim([FromUri] int projectId,
      [FromBody] CheckInCommand command)
    {
      var claim = await ClaimsRepository.GetClaim(projectId, command.ClaimId);
      if (claim == null)
      {
        throw new HttpResponseException(HttpStatusCode.NotFound);
      }
      await ClaimsService.CheckInClaim(projectId, command.ClaimId, command.MoneyPaid);
      return "OK";
    }
  }
}