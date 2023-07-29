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
}
