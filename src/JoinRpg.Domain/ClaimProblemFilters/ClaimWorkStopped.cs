using System;
using System.Collections.Generic;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
    internal class ClaimWorkStopped : IProblemFilter<Claim>
    {
        public IEnumerable<ClaimProblem> GetProblems(Claim claim)
        {
            if (claim.Project.Active)
            {
                yield break;
            }

            if (!claim.IsApproved) // Our concern is only approved claims
            {
                yield break;
            }

            if (DateTime.UtcNow.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) //If filed only recently, do nothing
            {
                yield break;
            }

            if (claim.LastVisibleMasterCommentAt is null)
            {
                yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered, ProblemSeverity.Error);
            }

            else if (!claim.HasMasterCommentsInLastXDays(60))
            {
                yield return new ClaimProblem(ClaimProblemType.ClaimWorkStopped, ProblemSeverity.Hint, claim.LastVisibleMasterCommentAt.Value);
            }
        }
    }
}
