using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal class ClaimWorkStopped : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (!claim.IsApproved) // Our concern is only approved claims
      {
        yield break;
      }

      if (DateTime.UtcNow.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) //If filed only recently, do nothing
      {
        yield break;
      }

      if (!claim.GetMasterAnswers().Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered, ProblemSeverity.Error);
      }

      else if (!claim.GetMasterAnswers().InLastXDays(60).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimWorkStopped, ProblemSeverity.Hint, claim.GetMasterAnswers().Last().CreatedTime);
      }
    }
  }
}
