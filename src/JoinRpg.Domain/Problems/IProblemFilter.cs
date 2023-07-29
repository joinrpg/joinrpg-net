namespace JoinRpg.Domain.Problems;

public interface IProblemFilter<in TObject>
{
    IEnumerable<ClaimProblem> GetProblems(TObject claim);
}
