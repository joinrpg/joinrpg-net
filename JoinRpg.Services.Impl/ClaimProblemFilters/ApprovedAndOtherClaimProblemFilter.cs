using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  class ApprovedAndOtherClaimProblemFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
    {
      if (claim.IsInDiscussion && claim.Character?.ApprovedClaim != null)
      {
        yield return new ClaimProblem(claim, ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim);
      }
    }
  }
}
