namespace JoinRpg.Domain.Problems;

public interface IFieldRelatedProblemFilter<in TObject> where TObject : IFieldContainter
{
    IEnumerable<FieldRelatedProblem> CheckField(CharacterItem target, FieldWithValue fieldWithValue);
}
