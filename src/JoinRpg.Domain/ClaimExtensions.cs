using System.Diagnostics.Contracts;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

public static class ClaimExtensions
{

    public static IEnumerable<Claim> OtherPendingClaimsForThisPlayer(this Claim claim)
        => claim.Player.Claims.Where(c => c.ClaimId != claim.ClaimId && c.IsPending && c.ProjectId == claim.ProjectId);

    /// <summary>
    /// Returns true when approval is blocked by other claims for character of the current claim
    /// </summary>
    public static bool HasOtherClaimsForThisCharacter(this Claim claim)
    {
        if (claim.IsApproved)
        {
            return false;
        }
        if (claim.Character.CharacterType == PrimitiveTypes.CharacterType.Slot)
        {
            return false;
        }
        return claim.Character.Claims.Any(c => c.PlayerUserId != claim.PlayerUserId && c.ClaimStatus.IsActive());
    }

    public static bool IsPartOfAnyOfGroups(this Character claimSource, IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        //TODO we can do faster than this
        return CharacterGroupIdentification.FromList(claimSource.GetParentGroupsToTop().Select(x => x.CharacterGroupId), new ProjectIdentification(claimSource.ProjectId)).Intersect(groups).Any();
    }

    public static bool IsPartOfGroup(this Character claimSource, int characterGroupId)
    {
        //TODO we can do faster than this
        return claimSource.GetParentGroupsToTop().Any(g => g.CharacterGroupId == characterGroupId);
    }

    public static void EnsureStatus(this Claim claim, params Claim.Status[] possibleStatus)
    {
        if (!possibleStatus.Contains(claim.ClaimStatus))
        {
            throw new ClaimWrongStatusException(claim, possibleStatus);
        }
    }

    [Pure]
    public static bool CanChangeTo(this Claim.Status fromStatus, Claim.Status targetStatus)
    {
        switch (targetStatus)
        {
            case Claim.Status.Approved:
                return new[] { Claim.Status.AddedByUser, Claim.Status.Discussed, Claim.Status.CheckedIn }.Contains(fromStatus);
            case Claim.Status.OnHold:
                return
                  new[] { Claim.Status.AddedByUser, Claim.Status.Discussed, Claim.Status.AddedByMaster }
                    .Contains(fromStatus);
            case Claim.Status.AddedByUser:
                return new[]
                    {Claim.Status.DeclinedByUser, Claim.Status.DeclinedByMaster, Claim.Status.OnHold}
                  .Contains(fromStatus);
            case Claim.Status.AddedByMaster:
                return false;
            case Claim.Status.DeclinedByUser:
            case Claim.Status.DeclinedByMaster:
                return
                  new[]
                  {
          Claim.Status.AddedByUser, Claim.Status.Discussed, Claim.Status.AddedByMaster,
          Claim.Status.Approved, Claim.Status.OnHold,
                  }.Contains(fromStatus);
            case Claim.Status.Discussed:
                return
                  new[] { Claim.Status.AddedByUser, Claim.Status.Discussed, Claim.Status.AddedByMaster }
                    .Contains(fromStatus);
            case Claim.Status.CheckedIn:
                return fromStatus == Claim.Status.Approved;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
        }
    }

    public static void EnsureCanChangeStatus(this Claim claim, Claim.Status targetStatus)
    {
        if (!claim.ClaimStatus.CanChangeTo(targetStatus))
        {
            throw new ClaimWrongStatusException(claim);
        }
    }

    public static IEnumerable<Claim> OfUserActive(this IEnumerable<Claim> enumerable, int? currentUserId) => enumerable.Where(c => c.PlayerUserId == currentUserId && c.ClaimStatus.IsActive());

    public static IEnumerable<Claim> OfUserApproved(this IEnumerable<Claim> enumerable, int currentUserId) => enumerable.Where(c => c.PlayerUserId == currentUserId && c.IsApproved);

    public static void ChangeStatusWithCheck(this Claim claim, Claim.Status targetStatus)
    {
        claim.EnsureCanChangeStatus(targetStatus);
        claim.ClaimStatus = targetStatus;
    }

    [Pure]
    public static Claim? TrySelectSingleClaim(this IReadOnlyCollection<Claim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);

        if (claims.Count(c => c.IsApproved) == 1)
        {
            return claims.Single(c => c.IsApproved);
        }
        if (claims.Count(c => c.IsInDiscussion) == 1)
        {
            return claims.Single(c => c.IsInDiscussion);
        }
        if (claims.Count == 1)
        {
            return claims.Single();
        }
        return null;
    }
}
