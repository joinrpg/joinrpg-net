using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Web.Filter;
using JoinRpg.Web.XGameApi.Contract;

namespace JoinRpg.Web.Controllers.XGameApi
{
  [RoutePrefix("x-game-api/{projectId}/checkin"), XGameAuthorize()]
  public class CheckInController : XGameApiController
  {
    [ProvidesContext]
    private IClaimsRepository ClaimsRepository { get; }

    public CheckInController(IProjectRepository projectRepository,
      IClaimsRepository claimsRepository) : base(projectRepository)
    {
      ClaimsRepository = claimsRepository;
    }

    [Route("allclaims")]
    public async Task<IEnumerable<ClaimHeaderInfo>> GetClaimsForCheckIn(int projectId)
    {
      return new[]
      {
        new ClaimHeaderInfo
        {
          ClaimId = 1111,
          CharacterName = "Арагорн, сын Арахорна",
          Player = new PlayerInfo()
          {
            NickName = "Лео",
            FullName = "Леонид Алексеевич Царев",
            OtherNicks = "ЛеоЦарев, Царев, Грязный Лео"
          },
        }
      };
    }

    [Route("{claimId}/prepare")]
    [HttpGet]
    public async Task<ClaimCheckInValidationResult> PrepareClaimFoCheckIn([FromUri] int projectId,
      [FromUri] int claimId)
    {
      return
        new ClaimCheckInValidationResult
        {
          ClaimId = 1111,
          CheckedIn = false,
          Approved = true,
          CheckInPossible = true,
          EverythingFilled = true,
          ClaimFeeBalance = 3000,
          Handouts = new[]
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
      return "Ok";
    }
  }
}