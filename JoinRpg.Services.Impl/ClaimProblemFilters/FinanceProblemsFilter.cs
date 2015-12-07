using System.Collections.Generic;
using System.Linq;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  public class FinanceProblemsFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Claim claim)
    {
      if (claim.FinanceOperations.Any(fo => fo.RequireModeration))
      {
        yield return new ClaimProblem(claim, ClaimProblemType.FinanceModerationRequired);
      }
      if (claim.ClaimTotalFee() < claim.ClaimBalance())
      {
        yield return new ClaimProblem(claim, ClaimProblemType.TooManyMoney);
      }
      if (claim.ClaimBalance() < claim.ClaimTotalFee() && claim.ClaimBalance() > 0)
      {
        yield return new ClaimProblem(claim, ClaimProblemType.FeePaidPartially);
      }
    }
  }
}
