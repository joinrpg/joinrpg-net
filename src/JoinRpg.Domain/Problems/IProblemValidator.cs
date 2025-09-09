namespace JoinRpg.Domain.Problems;
public interface IProblemValidator<TObject> where TObject : IFieldContainter
{
    IEnumerable<ClaimProblem> Validate(TObject claim, ProjectInfo projectInfo, ProblemSeverity minimalSeverity = ProblemSeverity.Hint);
    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo, IEnumerable<ProjectFieldIdentification> fields);
    IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo);

    IEnumerable<FieldRelatedProblem> ValidateFieldOnly(TObject claim, ProjectInfo projectInfo, ProjectFieldIdentification fieldId)
        => ValidateFieldsOnly(claim, projectInfo, new[] { fieldId });
}
