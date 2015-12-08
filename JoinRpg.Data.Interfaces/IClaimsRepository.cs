using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IClaimsRepository : IDisposable
  {
    Task<IEnumerable<Claim>>  GetActiveClaimsForUser(int userId);

    Task<Project> GetClaims(int projectId);

    Task<IEnumerable<Claim>> GetMyClaimsForProject(int userId, int projectId);
    Task<IEnumerable<Claim>>  GetClaimsByIds(int projectid, ICollection<int> claimindexes);
    Task<ICollection<Claim>> GetActiveClaims(int projectId);
    Task<ICollection<Claim>> GetActiveClaimsForMaster(int projectId, int userId);
  }
}
