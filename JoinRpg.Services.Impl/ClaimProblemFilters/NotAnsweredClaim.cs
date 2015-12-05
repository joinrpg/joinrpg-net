using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  class NotAnsweredClaim : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
    {
      if (!claim.IsInDiscussion)
      {
        yield break;
      }
      var now = DateTime.UtcNow;
      var masterAnswers =
        claim.Comments.Where(comment => !comment.IsCommentByPlayer && comment.IsVisibleToPlayer).ToList();
      var hasMasterCommentsInLast =
        masterAnswers
          .Any(comment => now.Subtract(comment.CreatedTime) < TimeSpan.FromDays(7));

      if (!masterAnswers.Any() &&
          now.Subtract(claim.CreateDate) > TimeSpan.FromDays(2))
      {
        yield return claim.Problem(ClaimProblemType.ClaimNeverAnswered, claim.CreateDate);
      }
      else if (!hasMasterCommentsInLast)
      {
        yield return claim.Problem(ClaimProblemType.ClaimDiscussionStopped, masterAnswers.Max(comment => comment.CreatedTime));
      }

        if (now.Subtract(claim.CreateDate) > TimeSpan.FromDays(30))
      {
        yield return claim.Problem(ClaimProblemType.ClaimNoDecision, claim.CreateDate);
      }
    }
  }
}
