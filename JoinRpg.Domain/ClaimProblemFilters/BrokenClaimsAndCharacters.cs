using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal class BrokenClaimsAndCharacters : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (claim.IsInDiscussion && claim.Character?.ApprovedClaim != null)
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim, ProblemSeverity.Error);
      }
      if (claim.IsApproved &&  (claim.Character == null || !claim.Character.IsActive))
      {
        yield return new ClaimProblem(ClaimProblemType.NoCharacterOnApprovedClaim, ProblemSeverity.Fatal);
      }
      if (claim.Character == null && claim.Group == null)
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimDontHaveTarget, ProblemSeverity.Fatal);
      }
    }
  }
}
