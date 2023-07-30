using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes.ProjectMetadata;

namespace JoinRpg.Domain.Problems.ClaimProblemFilters;

public class FinanceProblemsFilter : IProblemFilter<Claim>
{
    public IEnumerable<ClaimProblem> GetProblems(Claim claim, ProjectInfo projectInfo)
    {
        if (claim.FinanceOperations.Any(fo => fo.RequireModeration))
        {
            yield return new ClaimProblem(ClaimProblemType.FinanceModerationRequired, ProblemSeverity.Warning);
        }
        if (claim.ClaimTotalFee() < claim.ClaimBalance() && claim.Project.Details.FinanceWarnOnOverPayment)
        {
            yield return new ClaimProblem(ClaimProblemType.TooManyMoney, ProblemSeverity.Error);
        }
        if (!claim.ClaimPaidInFull() && claim.ClaimBalance() > 0)
        {
            yield return new ClaimProblem(ClaimProblemType.FeePaidPartially, ProblemSeverity.Hint);
        }
        if (claim.IsInDiscussion && claim.ClaimBalance() > 0)
        {
            yield return new ClaimProblem(ClaimProblemType.UnApprovedClaimPayment, ProblemSeverity.Warning);
        }
    }
}
