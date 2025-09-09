namespace JoinRpg.Domain.Problems.CommonProblemFilters;
internal class InActiveVariantsFilter : IFieldRelatedProblemFilter<Character>, IFieldRelatedProblemFilter<Claim>
{
    public IEnumerable<FieldRelatedProblem> CheckField(CharacterItem target, FieldWithValue fieldWithValue)
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
