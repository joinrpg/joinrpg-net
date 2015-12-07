using System;
using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  internal interface IClaimProblemFilter
  {
    IEnumerable<ClaimProblem> GetProblems(Claim claim);
  }

  internal static class ClaimProblemExts
  {
    public static ClaimProblem Problem(this Claim claim, ClaimProblemType type, DateTime? problemTime = null)
    {
      return new ClaimProblem(claim, type, problemTime);
    }
  }
}
