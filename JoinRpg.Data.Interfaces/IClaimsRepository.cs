using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public enum ClaimStatusSpec
  {
    Any, Active, InActive,
    Discussion,
    OnHold, Approved
  }
  public interface IClaimsRepository : IDisposable
  {
    Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status);

    Task<IEnumerable<Claim>> GetMyClaimsForProject(int userId, int projectId);
    Task<IEnumerable<Claim>>  GetClaimsByIds(int projectid, ICollection<int> claimindexes);
    Task<IReadOnlyCollection<Claim>> GetActiveClaimsForMaster(int projectId, int userId, ClaimStatusSpec status);
    Task<Claim> GetClaim(int projectId, int claimId);
    Task<Claim> GetClaimWithDetails(int projectId, int claimId);
  }
}
