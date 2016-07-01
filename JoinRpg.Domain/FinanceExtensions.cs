using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;

namespace JoinRpg.Domain
{
  public static class FinanceExtensions
  {
    public static int CurrentFee(this Project project)
    {
      return project.CurrentFee(DateTime.UtcNow);
    }

    private static int CurrentFee(this Project project, DateTime operationDate)
    {
      return project.ProjectFeeSettings.Where(pfs => pfs.StartDate < operationDate)
        .OrderByDescending(pfs => pfs.StartDate).FirstOrDefault()?.Fee ?? 0;
    }

    private static int ClaimTotalFee(this Claim claim, DateTime operationDate)
    {

      return claim.ClaimCurrentFee(operationDate) + claim.ApprovedFinanceOperations.Sum(fo => fo.FeeChange);
    }

    private static int ClaimCurrentFee(this Claim claim, DateTime operationDate)
      => claim.CurrentFee ?? claim.Project.CurrentFee(operationDate);

    public static int ClaimTotalFee(this Claim claim) => claim.ClaimTotalFee(DateTime.UtcNow);
    public static int ClaimCurrentFee(this Claim claim) => claim.ClaimCurrentFee(DateTime.UtcNow);

    public static int ClaimFeeDue(this Claim claim) => claim.ClaimTotalFee() - claim.ClaimBalance();

    public static int ClaimBalance(this Claim claim)
    {
      return claim.ApprovedFinanceOperations.Sum(fo => fo.MoneyAmount);
    }

    public static IEnumerable<PaymentType> GetPaymentTypes(this ProjectAcl acl)
    {
      return acl.Project.PaymentTypes.Where(pt => pt.UserId == acl.UserId);
    }

    //TODO[Localize]: JoinRpg.Domain should be localization-neutral. 
    public static string GetDisplayName([NotNull] this PaymentType paymentType)
    {
      if (paymentType == null) throw new ArgumentNullException(nameof(paymentType));
      return paymentType.IsCash ? "Наличными — " + paymentType.User.DisplayName : paymentType.Name;
    }

    public static void RequestModerationAccess(this FinanceOperation finance, int currentUserId)
    {
      if (!finance.Claim.HasMasterAccess(currentUserId, acl => acl.CanManageMoney) &&
          finance.PaymentType?.UserId != currentUserId)
      {
        throw new NoAccessToProjectException(finance, currentUserId);
      }
    }

    public static bool ClaimPaidInFull(this Claim claim) => claim.ClaimBalance() >= claim.ClaimTotalFee();

    private static bool ClaimPaidInFull(this Claim claim, DateTime operationDate)
      => claim.ClaimBalance() >= claim.ClaimTotalFee(operationDate.AddDays(-1));

    public static void UpdateClaimFeeIfRequired(this Claim claim, DateTime operationDate)
    {
      if (claim.Project.ProjectFeeSettings.Any() //If project has fee 
          && claim.CurrentFee == null //and fee not already fixed for claim
          && claim.ClaimPaidInFull(operationDate) //and current fee is payed in full
        )
      {
        claim.CurrentFee = claim.Project.CurrentFee(operationDate); //fix fee for claim
      }
    }
  }
}
