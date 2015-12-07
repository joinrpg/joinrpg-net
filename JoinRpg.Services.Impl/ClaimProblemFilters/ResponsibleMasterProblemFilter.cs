using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  class ResponsibleMasterProblemFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (claim.ResponsibleMasterUser == null)
      {
        yield return claim.Problem(ClaimProblemType.NoResponsibleMaster);
      }
      else if (!claim.HasMasterAccess(claim.ResponsibleMasterUserId))
      {
        yield return claim.Problem(ClaimProblemType.InvalidResponsibleMaster);
      }
    }
  }
}
