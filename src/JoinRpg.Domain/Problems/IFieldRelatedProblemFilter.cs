using JoinRpg.DataModel;

namespace JoinRpg.Domain.Problems;

public interface IFieldRelatedProblemFilter<in TObject> where TObject : IFieldContainter
{
    IEnumerable<FieldRelatedProblem> CheckField(Character target, FieldWithValue fieldWithValue);
}
