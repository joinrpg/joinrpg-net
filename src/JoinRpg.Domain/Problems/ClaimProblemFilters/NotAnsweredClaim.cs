using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class NotAnsweredClaim : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        var claim = context.Claim;
        var now = DateTime.UtcNow;

        if (!claim.IsInDiscussion) // Нас интересуют только обсуждаемые заявки
        {
            yield break;
        }

        if (now.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) // Только что поданную не трогаем
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
