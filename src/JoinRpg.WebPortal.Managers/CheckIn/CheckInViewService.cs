using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.Web.CheckIn;

namespace JoinRpg.WebPortal.Managers.CheckIn;

internal class CheckInViewService(IClaimsRepository claimsRepository) : ICheckInClient
{
    public async Task<CheckInStatViewModel> GetCheckInStats(ProjectIdentification projectId)
    {
        return new CheckInStatViewModel(
            CheckedInCount: (await claimsRepository.GetClaimHeadersWithPlayer(projectId, ClaimStatusSpec.CheckedIn)).Count,
            ReadyForCheckInCount: (await claimsRepository.GetClaimHeadersWithPlayer(projectId, ClaimStatusSpec.ReadyForCheckIn)).Count
        );
    }
}
