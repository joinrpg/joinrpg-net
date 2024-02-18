using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;
internal class ProblemValidator<TObject> : IProblemValidator<TObject> where TObject : IFieldContainter
{
    private readonly IEnumerable<IProblemFilter<TObject>> filters;

    public ProblemValidator(IEnumerable<IProblemFilter<TObject>> filters)
    {
        this.filters = filters;
        if (!filters.Any())
        {
            throw new InvalidOperationException($"Filters for type {typeof(TObject).FullName} do not exists");
        }
    }

    public IEnumerable<ClaimProblem> Validate(TObject claim, ProjectInfo projectInfo, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return filters
            .SelectMany(filter => filter.GetProblems(claim, projectInfo))
            .Where(problem => problem.Severity >= minimalSeverity);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo, IEnumerable<ProjectFieldIdentification> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        return ValidateFieldsOnly(claim, projectInfo).Where(fp => fields.Contains(fp.Field.Id));
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject claim, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(projectInfo);

        return Validate(claim, projectInfo).OfType<FieldRelatedProblem>();
    }
}
