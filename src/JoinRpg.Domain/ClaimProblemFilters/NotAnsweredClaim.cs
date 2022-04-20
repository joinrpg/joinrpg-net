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

            if (claim.LastVisibleMasterCommentAt is null)
            {
                yield return new ClaimProblem(ClaimProblemType.ClaimNeverAnswered, ProblemSeverity.Error, claim.CreateDate);
            }
            else if (!claim.HasMasterCommentsInLastXDays(14))
            {
                yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Error, claim.LastVisibleMasterCommentAt.Value);
            }
            else if (!claim.HasMasterCommentsInLastXDays(7))
            {
                yield return new ClaimProblem(ClaimProblemType.ClaimDiscussionStopped, ProblemSeverity.Warning, claim.LastVisibleMasterCommentAt.Value);
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
