using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public enum ClaimStatusSpec
  {
    Any, Active, InActive,
    Discussion,
    OnHold, Approved,
    ReadyForCheckIn,
    CheckedIn
  }

  public class ClaimCountByMaster
  {
    public int? MasterId { get; set; }
    public int ClaimCount { get; set; }
  }
  public interface IClaimsRepository : IDisposable
  {
    Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status);

    Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, IReadOnlyCollection<int> claimindexes);
    Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status);
    [ItemCanBeNull]
    Task<Claim> GetClaim(int projectId, int? claimId);
    [ItemCanBeNull]
    Task<Claim> GetClaimWithDetails(int projectId, int claimId);
    Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(int projectId, ClaimStatusSpec active, int[] characterGroupsIds);
    Task<IReadOnlyCollection<Claim>> GetClaimsForGroupDirect(int projectId, ClaimStatusSpec active, int characterGroupsId);
    Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId);

    Task<Claim> GetClaimByDiscussion(int projectId, int commentDiscussionId);
    
    [NotNull, ItemNotNull]
    Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec);

    Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(int projectId, ClaimStatusSpec approved);
  }
}
