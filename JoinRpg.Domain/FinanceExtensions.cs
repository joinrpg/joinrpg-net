using System;
using System.Linq;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FinanceExtensions
  {
    public static int CurrentFee(this Project project, DateTime operationDate)
    {
      return project.ProjectFeeSettings.Where(pfs => pfs.EndDate > operationDate)
        .OrderBy(pfs => pfs.EndDate).FirstOrDefault()?.Fee ?? 0;
    }

    public static int ClaimTotalFee(this Claim claim, DateTime operationDate)
    {

      return claim.ClaimCurrentFee(operationDate) + claim.FinanceOperations.Sum(fo => fo.FeeChange);
    }

    public static int ClaimCurrentFee(this Claim claim, DateTime operationDate)
      => claim.CurrentFee ?? claim.Project.CurrentFee(operationDate);

    public static int ClaimTotalFee(this Claim claim) => claim.ClaimTotalFee(DateTime.UtcNow);
    public static int ClaimCurrentFee(this Claim claim) => claim.ClaimCurrentFee(DateTime.UtcNow);

    public static int ClaimBalance(this Claim claim)
    {
      return claim.FinanceOperations.Sum(fo => fo.MoneyAmount);
    }
  }
}
