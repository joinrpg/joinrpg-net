using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  class ResponsibleMasterProblemFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
    {
      if (claim.ResponsibleMasterUser == null)
      {
        yield return claim.Problem(ClaimProblemType.NoResponsibleMaster);
      }
      else if (!project.HasAccess(claim.ResponsibleMasterUserId))
      {
        yield return claim.Problem(ClaimProblemType.InvalidResponsibleMaster);
      }
    }

   
  }
}
