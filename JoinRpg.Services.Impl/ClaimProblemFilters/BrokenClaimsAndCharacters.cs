using System.Collections.Generic;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  internal class BrokenClaimsAndCharacters : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
    {
      if (claim.IsInDiscussion && claim.Character?.ApprovedClaim != null)
      {
        yield return new ClaimProblem(claim, ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim);
      }
      if (claim.IsActive &&  (claim.Character == null || !claim.Character.IsActive))
      {
        yield return new ClaimProblem(claim, ClaimProblemType.NoCharacterOnApprovedClaim);
      }
    }
  }
}
