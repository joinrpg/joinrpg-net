using System.Data.Entity.SqlServer;
using System.Linq.Expressions;
using JoinRpg.Data.Interfaces.Claims;
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
                                c.ClaimStatus == Claim.Status.DeclinedByUser ||
                                c.ClaimStatus == Claim.Status.OnHold;
                case ClaimStatusSpec.Discussion:
                    return c => c.ClaimStatus == Claim.Status.AddedByMaster ||
                                c.ClaimStatus == Claim.Status.AddedByUser ||
                                c.ClaimStatus == Claim.Status.Discussed;
                case ClaimStatusSpec.OnHold:
                    return c => c.ClaimStatus == Claim.Status.OnHold;
                case ClaimStatusSpec.Approved:
                    return c =>
                        c.ClaimStatus == Claim.Status.Approved ||
                        c.ClaimStatus == Claim.Status.CheckedIn;
                case ClaimStatusSpec.ReadyForCheckIn:
                    return c => c.ClaimStatus == Claim.Status.Approved && c.CheckInDate == null;
                case ClaimStatusSpec.CheckedIn:
                    return c => c.ClaimStatus == Claim.Status.CheckedIn;
                case ClaimStatusSpec.ActiveOrOnHold:
                    return c => c.ClaimStatus != Claim.Status.DeclinedByMaster &&
                                c.ClaimStatus != Claim.Status.DeclinedByUser;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status), status, null);
            }
        }

        public static Expression<Func<Claim, bool>> GetResponsible(int masterUserId) => claim => claim.ResponsibleMasterUserId == masterUserId;

        public static Expression<Func<Claim, bool>> GetMyClaim(int userId) => claim => claim.PlayerUserId == userId;

        public static Expression<Func<Claim, bool>> GetInGroupPredicate(int[] characterGroupsIds) =>
            claim => (claim.CharacterGroupId != null && characterGroupsIds.Contains(claim.CharacterGroupId.Value))
                        ||
                        (claim.Character != null &&
                        characterGroupsIds.Any(id => SqlFunctions.CharIndex(id.ToString(), claim.Character.ParentGroupsImpl.ListIds) > 0
                         ));
    }
}
