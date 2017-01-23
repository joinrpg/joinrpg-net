using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class ClaimExtensions
  {
    public static bool HasAnyAccess(this Claim claim, int? currentUserId)
    {
      return claim.PlayerUserId == currentUserId || claim.HasMasterAccess(currentUserId);
    }

    public static IEnumerable<Claim> OtherPendingClaimsForThisPlayer(this Claim claim)
    {
      return claim.Player.Claims.Where(c => c.ClaimId != claim.ClaimId && c.IsPending && c.ProjectId == claim.ProjectId);
    }

    /// <summary>
    /// If claims for group, not for character, this is 0
    /// </summary>
    /// <param name="claim"></param>
    /// <returns></returns>
    [NotNull, ItemNotNull]
    public static IEnumerable<Claim> OtherClaimsForThisCharacter([NotNull] this Claim claim)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      return claim.Character?.Claims?.Where(c => c.PlayerUserId != claim.PlayerUserId && c.IsActive) ?? new List<Claim>();
    }

    [NotNull]
    public static IClaimSource GetTarget([NotNull] this Claim claim)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (claim.Character == null && claim.Group == null)
      {
        throw new InvalidOperationException("Claim not bound neither to character nor character group. That shouldn't happen");
      }
      return (IClaimSource) claim.Character ?? claim.Group;
    }

    public static IEnumerable<CharacterGroup> GetGroupsPartOf(this IClaimSource claimSource)
    {
      return claimSource
        .GetParentGroupsToTop() //Get parents
        .Union(claimSource as CharacterGroup) //Don't forget group himself
        .WhereNotNull();
    }

    public static bool IsPartOfGroup(this Claim cl, int characterGroupId)
    {
      return cl.GetTarget().IsPartOfGroup(characterGroupId);
    }

    public static bool IsPartOfAnyOfGroups(this IClaimSource claimSource, IEnumerable<CharacterGroup> groups)
    {
      //TODO we can do faster than this
      return claimSource.GetGroupsPartOf().Intersect(groups).Any();
    }

    public static bool IsPartOfGroup(this IClaimSource claimSource, int characterGroupId)
    {
//TODO we can do faster than this
      return claimSource.GetGroupsPartOf().Any(g => g.CharacterGroupId == characterGroupId);
    }

    public static void EnsureStatus(this Claim claim, params Claim.Status[] possibleStatus)
    {
      if (!possibleStatus.Contains(claim.ClaimStatus))
      {
        throw new ClaimWrongStatusException(claim, possibleStatus);
      }
    }

    public static bool CanChangeTo(this Claim.Status fromStatus, Claim.Status targetStatus)
    {
      switch (targetStatus)
      {
        case Claim.Status.Approved:
          return new[] {Claim.Status.AddedByUser, Claim.Status.Discussed}.Contains(fromStatus);
        case Claim.Status.OnHold:
          return
            new[] {Claim.Status.AddedByUser, Claim.Status.Discussed, Claim.Status.AddedByMaster}.Contains(fromStatus);
      }
      throw new ArgumentException("Not implemented for target status", nameof(targetStatus));
    }

    public static void EnsureCanChangeStatus(this Claim claim, Claim.Status targetStatus)
    {
      if (!claim.ClaimStatus.CanChangeTo(targetStatus))
      {
        throw new ClaimWrongStatusException(claim);
      }
    }

    public static Claim RequestAccess([CanBeNull] this Claim claim, int currentUserId)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (!claim.HasAnyAccess(currentUserId))
      {
        throw new NoAccessToProjectException(claim, currentUserId);
      }
      return claim;
    }

    public static Claim RequestPlayerAccess([NotNull] this Claim claim, int currentUserId)
    {
      if (claim == null) throw new ArgumentNullException(nameof(claim));
      if (!claim.HasPlayerAccesToClaim(currentUserId))
      {
        throw new NoAccessToProjectException(claim, currentUserId);
      }
      return claim;
    }

    public static IEnumerable<Comment> GetMasterAnswers(this CommentDiscussion claim)
    {
      return claim.Comments.Where(comment => !comment.IsCommentByPlayer && comment.IsVisibleToPlayer);
    }

    public static IEnumerable<Claim> OfUserActive(this IEnumerable<Claim> enumerable, int? currentUserId)
    {
      return enumerable.Where(c => c.PlayerUserId == currentUserId && c.IsActive);
    }

    public static IEnumerable<Claim> OfUserApproved(this IEnumerable<Claim> enumerable, int currentUserId)
    {
      return enumerable.Where(c => c.PlayerUserId == currentUserId && c.IsApproved);
    }

    public static void ChangeStatusWithCheck(this Claim claim, Claim.Status targetStatus)
    {
      claim.EnsureCanChangeStatus(targetStatus);
      claim.ClaimStatus = targetStatus;
    }

    public static bool CanManageClaim(this Claim claim, int currentUserId)
    {
      return claim.HasMasterAccess(currentUserId, acl => acl.CanManageClaims) ||
             claim.ResponsibleMasterUserId == currentUserId;
    }

    [CanBeNull]
    public static Claim TrySelectSingleClaim([NotNull, ItemNotNull] this IReadOnlyCollection<Claim> claims)
    {
      if (claims == null) throw new ArgumentNullException(nameof(claims));

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
}