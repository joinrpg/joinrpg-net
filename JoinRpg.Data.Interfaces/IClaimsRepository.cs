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
  }
}
