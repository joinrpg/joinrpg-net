using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;

public class FieldRelatedProblem : ClaimProblem
{
    public ProjectFieldInfo Field { get; }

    public FieldRelatedProblem(ClaimProblemType problemType, ProblemSeverity severity, ProjectFieldInfo field)
      : base(problemType, severity, field.Name) => Field = field ?? throw new ArgumentNullException(nameof(field));
}
