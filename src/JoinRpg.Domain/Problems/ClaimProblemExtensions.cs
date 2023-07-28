using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Domain.Problems.ClaimProblemFilters;

namespace JoinRpg.Domain.Problems;

public static class ClaimProblemExtensions
{
    private static IProblemFilter<Claim>[] Filters { get; }

    public static IEnumerable<ClaimProblem> GetProblems([NotNull] this Claim claim, ProblemSeverity minimalSeverity = ProblemSeverity.Hint)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        return Filters.SelectMany(f => f.GetProblems(claim)).Where(p => p.Severity >= minimalSeverity);
    }

    static ClaimProblemExtensions()
    {
        Filters = new IProblemFilter<Claim>[]
        {
    new ResponsibleMasterProblemFilter(), new NotAnsweredClaim(), new BrokenClaimsAndCharacters(),
    new FinanceProblemsFilter(), new ClaimWorkStopped(), new FieldNotSetFilterClaim(),
        };
    }

    public static bool HasProblemsForFields([NotNull] this Claim claim, [NotNull, ItemNotNull] IEnumerable<ProjectField> fields)
    {
        if (claim == null)
        {
            throw new ArgumentNullException(nameof(claim));
        }

        if (fields == null)
        {
            throw new ArgumentNullException(nameof(fields));
        }

        return claim.GetProblems().OfType<FieldRelatedProblem>().Any(fp => fields.Select(f => f.ProjectFieldId).Contains(fp.Field.ProjectFieldId));
    }
}
