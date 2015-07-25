using System;
using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Data.Interfaces
{
  public interface IClaimsRepository : IDisposable
  {
    IEnumerable<Claim> GetClaimsForUser(int userId);
  }
}
