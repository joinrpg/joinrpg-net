using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

/// <summary>
/// Что мешает принять заявку автоматически, когда проект включил автоприём
/// (<c>ProjectClaimSettings.AutoAcceptClaims</c>): игроку показывают, почему автоприём ещё
/// не сработал. Условия считает <see cref="ClaimAutoAccept"/> — единый источник правды,
/// на него же переводится и сам автоприём в сервисе заявок.
/// </summary>
/// <remarks>
/// Показывает только исчерпанный лимит шаблона: блокер «не выдан доступ к защищённым данным»
/// уже показывается проблемой <see cref="ClaimProblemType.SensitiveDataNotAllowed"/>
/// (<see cref="ClaimContactsMissingFilter"/>), и для заявки на не-шаблон автоприём не действует
/// вовсе — там игрок ничего не может исправить, и задача «показать, что не хватило» не встаёт.
/// </remarks>
internal class AutoAcceptProblemFilter : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        if (!ClaimAutoAccept.AppliesTo(context)
            || !ClaimAutoAccept.GetBlockers(context).Contains(AutoAcceptBlocker.SlotExhausted))
        {
            yield break;
        }

        yield return new AutoAcceptRelatedProblem(ClaimProblemType.AutoAcceptSlotExhausted, ProblemSeverity.Warning);
    }
}
