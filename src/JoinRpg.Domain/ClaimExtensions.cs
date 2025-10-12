using System.Diagnostics.Contracts;
using JoinRpg.PrimitiveTypes.Claims;

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
        if (claim.Character.CharacterType == CharacterType.Slot)
        {
            return false;
        }
        return claim.Character.Claims.Any(c => c.PlayerUserId != claim.PlayerUserId && c.ClaimStatus.IsActive());
    }

    public static bool IsPartOfAnyOfGroups(this Character character, IReadOnlyCollection<CharacterGroupIdentification> groups)
    {
        //TODO we can do faster than this
        return CharacterGroupIdentification.FromList(character.GetParentGroupsToTop().Select(x => x.CharacterGroupId), new ProjectIdentification(character.ProjectId)).Intersect(groups).Any();
    }

    public static bool IsPartOfGroup(this Character character, int characterGroupId)
    {
        //TODO we can do faster than this
        return character.GetParentGroupsToTop().Any(g => g.CharacterGroupId == characterGroupId);
    }

    public static void EnsureStatus(this Claim claim, params ClaimStatus[] possibleStatus)
    {
        if (!possibleStatus.Contains(claim.ClaimStatus))
        {
            throw new ClaimWrongStatusException(claim, possibleStatus);
        }
    }

    [Pure]
    public static bool CanChangeTo(this ClaimStatus fromStatus, ClaimStatus targetStatus)
    {
        switch (targetStatus)
        {
            case ClaimStatus.Approved:
                return new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.CheckedIn }.Contains(fromStatus);
            case ClaimStatus.OnHold:
                return
                  new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster }
                    .Contains(fromStatus);
            case ClaimStatus.AddedByUser:
                return new[]
                    {ClaimStatus.DeclinedByUser, ClaimStatus.DeclinedByMaster, ClaimStatus.OnHold}
                  .Contains(fromStatus);
            case ClaimStatus.AddedByMaster:
                return false;
            case ClaimStatus.DeclinedByUser:
            case ClaimStatus.DeclinedByMaster:
                return
                  new[]
                  {
          ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster,
          ClaimStatus.Approved, ClaimStatus.OnHold,
                  }.Contains(fromStatus);
            case ClaimStatus.Discussed:
                return
                  new[] { ClaimStatus.AddedByUser, ClaimStatus.Discussed, ClaimStatus.AddedByMaster }
                    .Contains(fromStatus);
            case ClaimStatus.CheckedIn:
                return fromStatus == ClaimStatus.Approved;
            default:
                throw new ArgumentOutOfRangeException(nameof(targetStatus), targetStatus, null);
        }
    }

    public static void EnsureCanChangeStatus(this Claim claim, ClaimStatus targetStatus)
    {
        if (!claim.ClaimStatus.CanChangeTo(targetStatus))
        {
            throw new ClaimWrongStatusException(claim);
        }
    }

    public static IEnumerable<Claim> OfUserActive(this IEnumerable<Claim> enumerable, int? currentUserId) => enumerable.Where(c => c.PlayerUserId == currentUserId && c.ClaimStatus.IsActive());

    public static IEnumerable<Claim> OfUserApproved(this IEnumerable<Claim> enumerable, int currentUserId) => enumerable.Where(c => c.PlayerUserId == currentUserId && c.IsApproved);

    public static void ChangeStatusWithCheck(this Claim claim, ClaimStatus targetStatus)
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
