using System.Data.Entity.SqlServer;
using JoinRpg.PrimitiveTypes.Claims;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal static class ClaimPredicates
{
    public static Expression<Func<Claim, bool>> GetClaimStatusPredicate(ClaimStatusSpec status)
    {
        return status switch
        {
            ClaimStatusSpec.Any => claim => true,
            ClaimStatusSpec.Active => c => c.ClaimStatus != ClaimStatus.DeclinedByMaster &&
                                        c.ClaimStatus != ClaimStatus.DeclinedByUser &&
                                        c.ClaimStatus != ClaimStatus.OnHold,
            ClaimStatusSpec.InActive => c => c.ClaimStatus == ClaimStatus.DeclinedByMaster ||
                                        c.ClaimStatus == ClaimStatus.DeclinedByUser ||
                                        c.ClaimStatus == ClaimStatus.OnHold,
            ClaimStatusSpec.Discussion => c => c.ClaimStatus == ClaimStatus.AddedByMaster ||
                                        c.ClaimStatus == ClaimStatus.AddedByUser ||
                                        c.ClaimStatus == ClaimStatus.Discussed,
            ClaimStatusSpec.OnHold => c => c.ClaimStatus == ClaimStatus.OnHold,
            ClaimStatusSpec.Approved => c =>
                                c.ClaimStatus == ClaimStatus.Approved ||
                                c.ClaimStatus == ClaimStatus.CheckedIn,
            ClaimStatusSpec.ReadyForCheckIn => c => c.ClaimStatus == ClaimStatus.Approved && c.CheckInDate == null,
            ClaimStatusSpec.CheckedIn => c => c.ClaimStatus == ClaimStatus.CheckedIn,
            ClaimStatusSpec.ActiveOrOnHold => c => c.ClaimStatus != ClaimStatus.DeclinedByMaster &&
                                        c.ClaimStatus != ClaimStatus.DeclinedByUser,
            _ => throw new ArgumentOutOfRangeException(nameof(status), status, null),
        };
    }

    public static Expression<Func<Claim, bool>> GetResponsible(int masterUserId) => claim => claim.ResponsibleMasterUserId == masterUserId;

    [Obsolete]
    public static Expression<Func<Claim, bool>> GetForUser(int userId) => claim => claim.PlayerUserId == userId;

    public static Expression<Func<Claim, bool>> GetForUser(UserIdentification userId)
    {
        var id = userId.Value;
        return claim => claim.PlayerUserId == id;
    }

    public static Expression<Func<Claim, bool>> GetForProject(ProjectIdentification projectid)
    {
        var id = projectid.Value;
        return claim => claim.ProjectId == id;
    }

    public static Expression<Func<Claim, bool>> GetInGroupPredicate(IReadOnlyCollection<int> characterGroupsIds) =>
        claim => characterGroupsIds.Any(id => SqlFunctions.CharIndex(id.ToString(), claim.Character.ParentGroupsImpl.ListIds) > 0);

    public static Expression<Func<Claim, bool>> GetInGroupPredicate(IReadOnlyCollection<CharacterGroupIdentification> characterGroupsIds)
    {
        var (projectId, ids) = characterGroupsIds.ToIntListSameProject();
        var builder = PredicateBuilder.New<Claim>()
            .And(GetForProject(projectId))
            .And(GetInGroupPredicate(ids));
        return builder;
    }

    internal static Expression<Func<Claim, bool>> ByUgStatus(UgStatusSpec spec)
    {
        return spec switch
        {
            UgStatusSpec.Active => GetClaimStatusPredicate(ClaimStatusSpec.Active),
            UgStatusSpec.Vacant => GetClaimStatusPredicate(ClaimStatusSpec.Active),
            UgStatusSpec.Discussion => GetClaimStatusPredicate(ClaimStatusSpec.Active),
            UgStatusSpec.Archive => GetClaimStatusPredicate(ClaimStatusSpec.InActive),
            _ => throw new NotImplementedException(),
        };
    }
}
