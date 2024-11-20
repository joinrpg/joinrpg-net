using System.Data.Entity.SqlServer;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class ClaimPredicates
{
    public static Expression<Func<Claim, bool>> GetClaimStatusPredicate(ClaimStatusSpec status)
    {
        return status switch
        {
            ClaimStatusSpec.Any => claim => true,
            ClaimStatusSpec.Active => c => c.ClaimStatus != Claim.Status.DeclinedByMaster &&
                                        c.ClaimStatus != Claim.Status.DeclinedByUser &&
                                        c.ClaimStatus != Claim.Status.OnHold,
            ClaimStatusSpec.InActive => c => c.ClaimStatus == Claim.Status.DeclinedByMaster ||
                                        c.ClaimStatus == Claim.Status.DeclinedByUser ||
                                        c.ClaimStatus == Claim.Status.OnHold,
            ClaimStatusSpec.Discussion => c => c.ClaimStatus == Claim.Status.AddedByMaster ||
                                        c.ClaimStatus == Claim.Status.AddedByUser ||
                                        c.ClaimStatus == Claim.Status.Discussed,
            ClaimStatusSpec.OnHold => c => c.ClaimStatus == Claim.Status.OnHold,
            ClaimStatusSpec.Approved => c =>
                                c.ClaimStatus == Claim.Status.Approved ||
                                c.ClaimStatus == Claim.Status.CheckedIn,
            ClaimStatusSpec.ReadyForCheckIn => c => c.ClaimStatus == Claim.Status.Approved && c.CheckInDate == null,
            ClaimStatusSpec.CheckedIn => c => c.ClaimStatus == Claim.Status.CheckedIn,
            ClaimStatusSpec.ActiveOrOnHold => c => c.ClaimStatus != Claim.Status.DeclinedByMaster &&
                                        c.ClaimStatus != Claim.Status.DeclinedByUser,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }

    public static Expression<Func<Claim, bool>> GetResponsible(int masterUserId) => claim => claim.ResponsibleMasterUserId == masterUserId;

    public static Expression<Func<Claim, bool>> GetMyClaim(int userId) => claim => claim.PlayerUserId == userId;

    public static Expression<Func<Claim, bool>> GetInGroupPredicate(int[] characterGroupsIds) =>
        claim => characterGroupsIds.Any(id => SqlFunctions.CharIndex(id.ToString(), claim.Character.ParentGroupsImpl.ListIds) > 0);
}
