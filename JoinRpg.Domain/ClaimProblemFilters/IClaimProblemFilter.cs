using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal interface IClaimProblemFilter
  {
    IEnumerable<ClaimProblem> GetProblems(Claim claim);
  }
}
