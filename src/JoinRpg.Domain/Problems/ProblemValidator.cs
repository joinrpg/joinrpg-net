using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems;
internal class ProblemValidator<TObject>(
    IProblemFilter<TObject>[] filters,
    IFieldRelatedProblemFilter<TObject>[] fieldFilters
    ) : IProblemValidator<TObject> where TObject : IFieldContainter
{
    private readonly IProblemFilter<TObject>[] filters = ProblemValidator<TObject>.ShouldBeNotEmpty(filters);

    public IEnumerable<ClaimProblem> Validate(TObject claim, ProjectInfo projectInfo, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        ArgumentNullException.ThrowIfNull(claim);
        ArgumentNullException.ThrowIfNull(projectInfo);

        var problems = filters.SelectMany(filter => filter.GetProblems(claim, projectInfo));
        var fieldProblems = ValidateFieldsOnly(claim, projectInfo);

        return problems.Union(fieldProblems).Where(problem => problem.Severity >= minimalSeverity);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject obj, ProjectInfo projectInfo, IEnumerable<ProjectFieldIdentification> fields)
    {
        ArgumentNullException.ThrowIfNull(fields);

        FieldWithValue[] fieldWithValues = GetFields(obj, projectInfo).Where(f => fields.Contains(f.Field.Id)).ToArray();

        return ValidateFieldsInternal(obj, fieldWithValues);
    }

    public IEnumerable<FieldRelatedProblem> ValidateFieldsOnly(TObject obj, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(obj);
        ArgumentNullException.ThrowIfNull(projectInfo);

        FieldWithValue[] fieldWithValues = GetFields(obj, projectInfo);
        return ValidateFieldsInternal(obj, fieldWithValues);
    }

    private IEnumerable<FieldRelatedProblem> ValidateFieldsInternal(TObject obj, FieldWithValue[] fieldWithValues)
    {
        var target = GetClaimSource(obj);

        foreach (var fieldWithValue in fieldWithValues)
        {
            foreach (var problem in ValidateField(target, fieldWithValue))
            {
                yield return problem;
            }
        }
    }

    private IEnumerable<FieldRelatedProblem> ValidateField(IClaimSource target, FieldWithValue fieldWithValue)
    {
        foreach (var filter in fieldFilters)
        {
            foreach (var problem in filter.CheckField(target, fieldWithValue))
            {
                yield return problem;
            }
        }
    }

    private static FieldWithValue[] GetFields(TObject obj, ProjectInfo projectInfo)
    {
        return obj switch
        {
            Claim claim => claim.GetFields(projectInfo).Where(pf => pf.Field.BoundTo == FieldBoundTo.Claim || claim.IsApproved).ToArray(),
            Character character => character.GetFields(projectInfo).Where(pf => pf.Field.BoundTo == FieldBoundTo.Character || character.ApprovedClaim != null).ToArray(),
            _ => throw new NotImplementedException(),
        };
    }

    private static IClaimSource GetClaimSource(TObject obj)
    {
        return obj switch
        {
            Claim claim => claim.GetTarget(),
            Character character => character,
            _ => throw new NotImplementedException(),
        };
    }

    private static IProblemFilter<TObject>[] ShouldBeNotEmpty(IProblemFilter<TObject>[] filters)
    {
        return filters.Length > 0 ? filters :
            throw new InvalidOperationException($"Filters for type {typeof(TObject).FullName} do not exists");
    }
}
