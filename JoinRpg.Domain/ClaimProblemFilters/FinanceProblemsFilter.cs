using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain.ClaimProblemFilters
{
  public class FinanceProblemsFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (claim.FinanceOperations.Any(fo => fo.RequireModeration))
      {
        yield return new ClaimProblem(ClaimProblemType.FinanceModerationRequired);
      }
      if (claim.ClaimTotalFee() < claim.ClaimBalance())
      {
        yield return new ClaimProblem(ClaimProblemType.TooManyMoney);
      }
      if (!claim.ClaimPaidInFull() && claim.ClaimBalance() > 0)
      {
        yield return new ClaimProblem(ClaimProblemType.FeePaidPartially);
      }
      if (!claim.IsApproved && claim.ClaimBalance() > 0)
      {
        yield return new ClaimProblem(ClaimProblemType.UnApprovedClaimPayment);
      }
    }
  }
}
