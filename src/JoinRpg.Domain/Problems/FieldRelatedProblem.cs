using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;

public class FieldRelatedProblem(ClaimProblemType problemType, ProblemSeverity severity, ProjectFieldInfo field, string? extraInfo = null)
    : ClaimProblem(problemType, severity, field.Name + extraInfo ?? "")
{
    public ProjectFieldInfo Field { get; } = field ?? throw new ArgumentNullException(nameof(field));
}
