using System;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories
{
  internal static class ClaimPredicates
  {
    public static Expression<Func<Claim, bool>> GetClaimStatusPredicate(ClaimStatusSpec status)
    {
      switch (status)
      {
        case ClaimStatusSpec.Any:
          return claim => true;
        case ClaimStatusSpec.Active:
          return c => c.ClaimStatus != Claim.Status.DeclinedByMaster &&
                      c.ClaimStatus != Claim.Status.DeclinedByUser &&
                      c.ClaimStatus != Claim.Status.OnHold;
        case ClaimStatusSpec.InActive:
          return c => c.ClaimStatus == Claim.Status.DeclinedByMaster ||
                      c.ClaimStatus == Claim.Status.DeclinedByUser &&
                      c.ClaimStatus == Claim.Status.OnHold;
        case ClaimStatusSpec.Discussion:
          return c => c.ClaimStatus == Claim.Status.AddedByMaster ||
                      c.ClaimStatus == Claim.Status.AddedByUser ||
                      c.ClaimStatus == Claim.Status.Discussed;
        case ClaimStatusSpec.OnHold:
          return c => c.ClaimStatus == Claim.Status.OnHold;
        case ClaimStatusSpec.Approved:
          return c => c.ClaimStatus == Claim.Status.Approved || c.ClaimStatus == Claim.Status.CheckedIn;
        case ClaimStatusSpec.ReadyForCheckIn:
          return c => c.ClaimStatus == Claim.Status.Approved && c.CheckInDate == null;
        case ClaimStatusSpec.CheckedIn:
          return c => c.ClaimStatus == Claim.Status.CheckedIn;
        default:
          throw new ArgumentOutOfRangeException(nameof(status), status, null);
      }
    }
  }
}