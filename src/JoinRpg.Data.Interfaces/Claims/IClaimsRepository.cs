using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Data.Interfaces.Claims;

public interface IClaimsRepository : IDisposable
{
    Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status);

    Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, IReadOnlyCollection<int> claimindexes);
    Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status);
    Task<Claim> GetClaim(int projectId, int? claimId);

    Task<Claim> GetClaim(ClaimIdentification claimId);
    Task<Claim> GetClaimWithDetails(int projectId, int claimId);
    Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(int projectId, ClaimStatusSpec active, int[] characterGroupsIds);
    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimsHeadersForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId);

    Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(int projectId, ClaimStatusSpec approved);
    Task<IReadOnlyCollection<Claim>> GetClaimsForRoomType(int projectId, ClaimStatusSpec claimStatusSpec, int? roomTypeId);

    Task<IReadOnlyCollection<Claim>> GetClaimsForMoneyTransfersListAsync(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<Dictionary<int, int>> GetUnreadDiscussionsForClaims(int projectId, ClaimStatusSpec claimStatusSpec, int userId, bool hasMasterAccess);

    Task<IReadOnlyCollection<UpdatedClaimDto>> GetUpdatedClaimsSince(DateTimeOffset since);
}

public record class UpdatedClaimDto(ClaimIdentification ClaimId, UserIdentification UserId, ProjectName ProjectName, string CharacterName);
