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
      if (claim.Comments.All(comment => comment.AuthorUserId == claim.PlayerUserId) &&
          DateTime.UtcNow.Subtract(claim.CreateDate) > TimeSpan.FromDays(2))
      {
        yield return claim.Problem(ClaimProblemType.ClaimNeverAnswered, claim.CreateDate);
      }
    }
  }
}
