namespace JoinRpg.Domain.Problems;

internal interface IProblemFilter<in TObject>
{
    IEnumerable<ClaimProblem> GetProblems(TObject claim);
}
