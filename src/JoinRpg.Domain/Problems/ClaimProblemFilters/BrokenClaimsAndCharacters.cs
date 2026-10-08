using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

/// <remarks>
/// Ветки «у заявки нет персонажа» здесь больше нет: в доменном агрегате заявка существует только
/// как элемент <see cref="CharacterInfo.Claims"/>, а <see cref="ClaimInCharacter"/> проверяет
/// это в конструкторе. Поэтому <c>ClaimProblemType.ClaimDontHaveTarget</c> недостижим по
/// построению и помечен <c>[Obsolete]</c> — как <c>DeletedFieldHasValue</c> рядом.
/// </remarks>
internal class BrokenClaimsAndCharacters : IClaimProblemFilter
{
    public IEnumerable<ClaimProblem> GetProblems(ClaimInfo context)
    {
        if (context.Claim.IsInDiscussion && context.Character.ApprovedClaimId is not null)
        {
            yield return new ClaimProblem(ClaimProblemType.ClaimActiveButCharacterHasApprovedClaim, ProblemSeverity.Error);
        }
        if (context.Claim.IsApproved && !context.Character.IsActive)
        {
            yield return new ClaimProblem(ClaimProblemType.NoCharacterOnApprovedClaim, ProblemSeverity.Fatal);
        }
    }
}
