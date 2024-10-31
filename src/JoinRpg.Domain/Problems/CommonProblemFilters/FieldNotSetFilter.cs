using System.Diagnostics.Contracts;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.CommonProblemFilters;

internal class FieldNotSetFilter : IFieldRelatedProblemFilter<Character>, IFieldRelatedProblemFilter<Claim>
{
    public IEnumerable<FieldRelatedProblem> CheckField(IClaimSource target, FieldWithValue fieldWithValue)
    {
        if (!fieldWithValue.Field.CanHaveValue)
        {
            yield break;
        }

        var isAvailableForTarget = fieldWithValue.Field.IsAvailableForTarget(target);
        var hasValue = fieldWithValue.HasEditableValue;

        if (hasValue)
        {
            if (isAvailableForTarget)
            {
                yield break;
            }

            if (fieldWithValue.Field.IsActive)
            {
                yield return FieldProblem(ClaimProblemType.FieldShouldNotHaveValue, ProblemSeverity.Hint, fieldWithValue);
            }
        }
        else
        {
            if (!isAvailableForTarget)
            {
                yield break;
            }

            switch (fieldWithValue.Field.MandatoryStatus)
            {
                case MandatoryStatus.Optional:
                    break;
                case MandatoryStatus.Recommended:
                    yield return FieldProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Hint, fieldWithValue);
                    break;
                case MandatoryStatus.Required:
                    yield return FieldProblem(ClaimProblemType.FieldIsEmpty, ProblemSeverity.Warning, fieldWithValue);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    [Pure]
    private static FieldRelatedProblem FieldProblem(ClaimProblemType problemType, ProblemSeverity severity, FieldWithValue fieldWithValue) => new FieldRelatedProblem(problemType, severity, fieldWithValue.Field);
}
