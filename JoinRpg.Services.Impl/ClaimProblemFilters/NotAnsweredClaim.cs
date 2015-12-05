using System;
using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  internal class NotAnsweredClaim : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
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

      
      var masterAnswers =
        claim.Comments.Where(comment => !comment.IsCommentByPlayer && comment.IsVisibleToPlayer).ToList();
      var hasMasterCommentsInLast =
        masterAnswers
          .Any(comment => now.Subtract(comment.CreatedTime) < TimeSpan.FromDays(7));

      if (!masterAnswers.Any())
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
