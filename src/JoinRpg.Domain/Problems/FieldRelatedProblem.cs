using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.Problems;

public class FieldRelatedProblem : ClaimProblem
{
    [NotNull]
    public ProjectField Field { get; }

    public FieldRelatedProblem(ClaimProblemType problemType, ProblemSeverity severity, [NotNull] ProjectField field)
      : base(problemType, severity, field.FieldName) => Field = field ?? throw new ArgumentNullException(nameof(field));
}
