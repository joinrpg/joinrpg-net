using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimWorkStopped : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimProblemContext context)
    {
        var claim = context.Claim;

        // Правило срабатывает только в архиве. Раньше условие было записано как
        // claim.Project.Active, а Project.Active == false — это ровно
        // ProjectLifecycleStatus.Archived (см. ProjectLoaderCommon.CreateStatus), то есть
        // ProjectInfo.IsActive.
        if (context.ProjectInfo.IsActive)
        {
            yield break;
        }

        if (!claim.IsApproved) // Нас интересуют только утверждённые заявки
        {
            yield break;
        }

        if (DateTime.UtcNow.Subtract(claim.CreateDate) < TimeSpan.FromDays(2)) // Только что поданную не трогаем
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
