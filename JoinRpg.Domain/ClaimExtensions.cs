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
    public static IEnumerable<Claim> OtherClaimsForThisCharacter(this Claim claim)
    {
      return claim.Character?.Claims?.Where(c => c.PlayerUserId != claim.PlayerUserId && c.IsActive) ?? new List<Claim>();
    }

    [NotNull]
    public static IClaimSource GetTarget(this Claim claim)
    {
      if (claim.Character == null && claim.Group == null)
      {
        throw new InvalidOperationException("Claim not bound neither to character nor character group. That shouldn't happen");
      }
      return (IClaimSource) claim.Character ?? claim.Group;
    }

    private static IEnumerable<CharacterGroup> GetGroupsPartOf(this IClaimSource claimSource)
    {
      var sourceAsGroup = claimSource as CharacterGroup;
      return claimSource
        .GetParentGroups() //Get parents
        .Union(sourceAsGroup) //Don't forget group himself
        .WhereNotNull();
    }

    public static bool IsPartOfGroup(this Claim cl, int characterGroupId)
    {
      return cl.GetTarget().IsPartOfGroup(characterGroupId);
    }

    public static bool IsPartOfGroup(this IClaimSource claimSource, CharacterGroup group)
    {
      if (group.IsRoot)
      {
        return true;
      }
      //TODO we can do faster than this
      return claimSource.GetGroupsPartOf().Contains(group);
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

    public static IEnumerable<User> GetSubscriptions(this Claim claim, Func<UserSubscription, bool> predicate,
      int initiatorUserId, [CanBeNull] IEnumerable<User> extraRecepients, bool isVisibleToPlayer)
    {
      return ((IEnumerable<CharacterGroup>) claim.GetTarget().GetGroupsPartOf()) //Get all groups for claim
        .SelectMany(g => g.Subscriptions) //get subscriptions on groups
        .Union(claim.Subscriptions) //subscribtions on claim
        .Union(claim.Character?.Subscriptions ?? new UserSubscription[] {}) //and on characters
        .Where(predicate) //type of subscribe (on new comments, on new claims etc.)
        .Select(u => u.User) //Select users
        .Union(claim.ResponsibleMasterUser) //Responsible master is always subscribed on everything
        .Union(claim.Player) //...and player himself also
        .Union(extraRecepients ?? Enumerable.Empty<User>()) //add extra recepients
        .Where(u => isVisibleToPlayer || u != claim.Player) //remove player if we doing something not player visible
        .Where(u => u != null && u.UserId != initiatorUserId) //Do not send mail to self (and also will remove nulls)
        .Distinct() //One user can be subscribed by multiple reasons
        ;
    }

    public static Claim RequestAccess([NotNull] this Claim claim, int currentUserId)
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

    public static IEnumerable<Comment> GetMasterAnswers(this Claim claim)
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
  }
}