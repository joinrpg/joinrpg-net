using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Domain.Problems.CommonProblemFilters;

internal class InActiveVariantsFilter : IFieldRelatedProblemFilter<CharacterInfo>, IFieldRelatedProblemFilter<CharacterClaimInfo>
{
    public IEnumerable<FieldRelatedProblem> CheckField(IFieldAvailabilityTarget target, FieldWithValue fieldWithValue)
    {
        foreach (var variant in fieldWithValue.GetDropdownValues())
        {
            if (!variant.IsActive)
            {
                yield return new FieldRelatedProblem(ClaimProblemType.InActiveVariant, ProblemSeverity.Warning, fieldWithValue.Field, "/" + variant.Label);
            }
        }
    }
}
