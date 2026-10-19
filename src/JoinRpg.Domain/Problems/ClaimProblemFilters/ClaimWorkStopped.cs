using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

internal class ClaimWorkStopped : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        var claim = context.Claim;

        // В архиве разбираться, остановилась ли работа по заявке, бессмысленно: игра кончилась.
        // Прежний код проверял claim.Project.Active и вёл себя ровно наоборот — молчал на живых
        // проектах и срабатывал в архиве, то есть единственный мастер, которому правило могло
        // пригодиться, его и не видел. Условие исправлено при переносе на доменные сущности.
        if (context.ProjectInfo.IsArchived)
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
