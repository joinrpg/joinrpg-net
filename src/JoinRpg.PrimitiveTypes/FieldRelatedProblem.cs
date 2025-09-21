using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.PrimitiveTypes;

public record class FieldRelatedProblem(ClaimProblemType ProblemType, ProblemSeverity Severity, ProjectFieldInfo Field, string? ExtraInfo = null)
    : ClaimProblem(ProblemType, Severity, Field.Name + ExtraInfo ?? "")
{
    public ProjectFieldInfo Field { get; } = Field ?? throw new ArgumentNullException(nameof(Field));
}
