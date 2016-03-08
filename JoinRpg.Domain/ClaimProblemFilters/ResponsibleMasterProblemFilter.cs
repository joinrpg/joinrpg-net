using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  class ResponsibleMasterProblemFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (claim.ResponsibleMasterUser == null)
      {
        yield return new ClaimProblem(ClaimProblemType.NoResponsibleMaster, ProblemSeverity.Error, null);
      }
      else if (!claim.HasMasterAccess(claim.ResponsibleMasterUserId))
      {
        yield return new ClaimProblem(ClaimProblemType.InvalidResponsibleMaster, ProblemSeverity.Error, null);
      }
    }
  }
}
