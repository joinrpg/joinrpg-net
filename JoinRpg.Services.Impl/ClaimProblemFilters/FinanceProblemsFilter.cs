using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JoinRpg.DataModel;
using JoinRpg.Domain;
using JoinRpg.Services.Interfaces;

namespace JoinRpg.Services.Impl.ClaimProblemFilters
{
  public class FinanceProblemsFilter : IClaimProblemFilter
  {
    public IEnumerable<ClaimProblem> GetProblems(Project project, Claim claim)
    {
      if (claim.FinanceOperations.Any(fo => fo.RequireModeration))
      {
        yield return new ClaimProblem(claim, ClaimProblemType.FinanceModerationRequired);
      }
      if (claim.ClaimTotalFee() < claim.ClaimBalance())
      {
        yield return new ClaimProblem(claim, ClaimProblemType.TooManyMoney);
      }
    }
  }
}
