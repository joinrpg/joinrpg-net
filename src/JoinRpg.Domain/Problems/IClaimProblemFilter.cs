using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems;

/// <summary>
/// Правило поиска проблем заявки поверх доменных сущностей (ADR013).
/// </summary>
/// <remarks>
/// Зеркало <see cref="ICharacterProblemFilter"/> для стороны заявки. Всё, что правилу нужно знать,
/// приходит одним <see cref="ClaimInfo"/>: EF-сущности <c>Claim</c> здесь больше нет.
/// </remarks>
public interface IClaimProblemFilter
{
    IEnumerable<ClaimProblem> GetProblems(ClaimInfo context);
}
