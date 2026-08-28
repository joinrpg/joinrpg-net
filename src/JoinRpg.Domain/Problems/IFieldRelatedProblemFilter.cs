using JoinRpg.DomainTypes.Characters;

namespace JoinRpg.Domain.Problems;

public interface IFieldRelatedProblemFilter<in TObject> where TObject : IFieldContainter
{
    IEnumerable<FieldRelatedProblem> CheckField(IFieldAvailabilityTarget target, FieldWithValue fieldWithValue);
}
