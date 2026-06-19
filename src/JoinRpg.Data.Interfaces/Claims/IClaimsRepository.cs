using JoinRpg.DataModel;
using JoinRpg.DomainTypes.Claims;

namespace JoinRpg.Data.Interfaces.Claims;

public interface IClaimsRepository : IDisposable
{
    Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status);

    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(UserIdentification userId, ClaimStatusSpec status);

    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(ProjectIdentification projectId, UserIdentification userId, ClaimStatusSpec status);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<ClaimIdentification> claimIds);
    Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status);

    Task<Claim?> GetClaim(ClaimIdentification claimId);
    Task<Claim?> GetClaimWithDetails(ClaimIdentification claimId);

    Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(ProjectIdentification projectId, ClaimStatusSpec active, CharacterGroupIdentification[] characterGroupsIds);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds, ClaimStatusSpec spec);
    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(ProjectIdentification projectId, ClaimStatusSpec claimStatusSpec, UserIdentification userId);

    Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(ProjectIdentification projectId, ClaimStatusSpec approved);
    Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId);

    Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess);

}
