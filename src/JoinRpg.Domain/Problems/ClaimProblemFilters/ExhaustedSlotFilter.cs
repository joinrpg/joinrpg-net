using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

/// <summary>
/// Незакрытая заявка висит на шаблоне, в котором не осталось свободных мест: принять её нельзя,
/// пока мастер не поднимет лимит — например, такое случается после приглашения сверх лимита
/// (#4743). Проблема помечена <see cref="ClaimProblem.IsAutoAcceptBlocker"/>: заявка-кандидат
/// с ней принята автоматически не будет.
/// </summary>
internal class ExhaustedSlotFilter : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        var slot = context.Character.CharacterTypeInfo;
        if (!slot.IsSlot || slot.SlotLimit != 0 || !context.Claim.IsPending)
        {
            yield break;
        }

        yield return new ClaimProblem(ClaimProblemType.SlotExhausted, ProblemSeverity.Warning);
    }
}
