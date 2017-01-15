using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  internal class NotAnsweredClaim : IProblemFilter<Claim>
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


      var masterAnswers = claim.Discussion.GetMasterAnswers().ToList();
      if (!masterAnswers.Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered, ProblemSeverity.Error, claim.CreateDate);
      }
      else if (!masterAnswers.InLastXDays(14).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Error, masterAnswers.Last().CreatedAt);
      }
      else if (!masterAnswers.InLastXDays(7).Any())
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Warning, masterAnswers.Last().CreatedAt);
      }

      if (now.Subtract(claim.CreateDate) > TimeSpan.FromDays(60))
      {
        yield return new ClaimProblem(ClaimProblemType.ClaimNoDecision, ProblemSeverity.Error, claim.CreateDate);
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
