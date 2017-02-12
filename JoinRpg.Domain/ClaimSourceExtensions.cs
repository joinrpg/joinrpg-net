﻿ using System;
 using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Domain
{
  public static class ClaimSourceExtensions
  {
    public static bool HasClaimForUser(this IClaimSource claimSource, int currentUserId)
    {
      return claimSource.Claims.OfUserActive(currentUserId).Any();
    }

    [NotNull, ItemNotNull]
    public static IEnumerable<User> GetResponsibleMasters([NotNull] this IClaimSource group, bool includeSelf = true)
    {
      if (@group == null) throw new ArgumentNullException(nameof(group));

      if (@group.ResponsibleMasterUser != null && includeSelf)
      {
        return new[] {@group.ResponsibleMasterUser};
      }
      var candidates = new HashSet<CharacterGroup>();
      var removedGroups = new HashSet<CharacterGroup>();
      var lookupGroups = new HashSet<CharacterGroup>(@group.ParentGroups);
      while (lookupGroups.Any())
      {
        var currentGroup = lookupGroups.First();
        lookupGroups.Remove(currentGroup); //Get next group

        if (removedGroups.Contains(currentGroup) || candidates.Contains(currentGroup))
        {
          continue;
        }

        if (currentGroup.ResponsibleMasterUserId != null)
        {
          candidates.Add(currentGroup);
          removedGroups.AddRange(currentGroup.FlatTree(c => c.ParentGroups, includeSelf: false)); 
          //Some group with set responsible master will shadow out all parents.
        }
        else
        {
          lookupGroups.AddRange(currentGroup.ParentGroups);
        }
      }
      return candidates.Except(removedGroups).Select(c => c.ResponsibleMasterUser);
    }

    private static void AddRange<T>(this ISet<T> set, IEnumerable<T> objectsToAdd)
    {
      foreach (var parentGroup in objectsToAdd)
      {
        set.Add(parentGroup);
      }
    }

    [NotNull]
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop([CanBeNull] this IClaimSource target)
    {
      return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups)) ?? Enumerable.Empty<CharacterGroup>();
    }

    [NotNull]
    public static IEnumerable<CharacterGroup> GetChildrenGroups([NotNull] this CharacterGroup target)
    {
      if (target == null) throw new ArgumentNullException(nameof(target));
      return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups));
    }

    public static bool HasActiveClaims(this IClaimSource target)
    {
      return target.Claims.Any(claim => claim.IsActive);
    }

    [CanBeNull]
    public static User GetResponsibleMaster([NotNull] this Character character)
    {
      if (character == null) throw new ArgumentNullException(nameof(character));
      return character.ApprovedClaim?.ResponsibleMasterUser ?? character.GetResponsibleMasters().FirstOrDefault();
    }

    public static void EnsureAvailable<T>(this T claimSource) where T : IClaimSource
    {
      if (!claimSource.IsAvailable)
      {
        throw new ClaimTargetIsNotAcceptingClaims();
      }
    }

    public static bool IsNpc(this IClaimSource target)
    {
      return (target as Character)?.IsAcceptingClaims == false;
    }
  }
}
