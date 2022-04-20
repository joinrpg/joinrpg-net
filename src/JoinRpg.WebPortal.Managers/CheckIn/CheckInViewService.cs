using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.PrimitiveTypes;
using JoinRpg.Web.CheckIn;

namespace JoinRpg.WebPortal.Managers.CheckIn
{
    internal class CheckInViewService : ICheckInClient
    {
        private readonly IClaimsRepository claimsRepository;

        public CheckInViewService(IClaimsRepository claimsRepository) => this.claimsRepository = claimsRepository;

        public async Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId)
        {
            return new CheckInStatViewModel(
                CheckedInCount: (await claimsRepository.GetClaimHeadersWithPlayer(projectId.Value, ClaimStatusSpec.CheckedIn)).Count,
                ReadyForCheckInCount: (await claimsRepository.GetClaimHeadersWithPlayer(projectId, ClaimStatusSpec.ReadyForCheckIn)).Count
            );
        }
    }
}
