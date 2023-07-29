using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;
public interface IProblemValidator<TObject>
{
    IEnumerable<ClaimProblem> Validate(TObject claim, ProjectInfo projectInfo, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);
    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo, IEnumerable<ProjectFieldIdentification> fields);
    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo);
}
