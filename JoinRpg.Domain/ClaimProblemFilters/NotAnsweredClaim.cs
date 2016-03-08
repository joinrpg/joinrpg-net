using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal class NotAnsweredClaim : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      var now = DateTime.UtcNow;

      if (!claim.IsInDiscussion) // Our concern is only discussed claims
      {
        yield break;
      }

      if (now.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) //If filed only recently, do nothing
      {
        yield break;
      }


      if (!claim.GetMasterAnswers().Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered, ProblemSeverity.Error, claim.CreateDate);
      }
      else if (!claim.GetMasterAnswers().InLastXDays(14).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Error, claim.GetMasterAnswers().Last().CreatedTime);
      }
      else if (!claim.GetMasterAnswers().InLastXDays(7).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Warning, claim.GetMasterAnswers().Last().CreatedTime);
      }

      if (now.Subtract(claim.CreateDate) > TimeSpan.FromDays(60))
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNoDecision, ProblemSeverity.Warning, claim.CreateDate);
      }
      else if (now.Subtract(claim.CreateDate) > TimeSpan.FromDays(30))
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNoDecision, ProblemSeverity.Warning, claim.CreateDate);
      }
      else if (now.Subtract(claim.CreateDate) > TimeSpan.FromDays(14))
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNoDecision, ProblemSeverity.Hint, claim.CreateDate);
      }
    }
  }
}
