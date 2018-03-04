 using System;
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
      return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups)).OrderBy(g => g.CharacterGroupId)
               .Distinct() ?? Enumerable.Empty<CharacterGroup>();
    }

    [NotNull, ItemNotNull]
    public static IEnumerable<CharacterGroup> GetChildrenGroups([NotNull] this CharacterGroup target)
    {
      if (target == null) throw new ArgumentNullException(nameof(target));
      return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups)).Distinct();
    }

    public static bool HasActiveClaims(this IClaimSource target)
    {
      return target.Claims.Any(claim => claim.ClaimStatus.IsActive());
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

    public static bool IsNpc([CanBeNull] this IClaimSource target)
    {
        return target is Character character && !character.IsAcceptingClaims && character.ApprovedClaim == null;
    }

      public static bool IsAcceptingClaims<T>(this T characterGroup)
          where T : IClaimSource
      {
          return !ValidateIfCanAddClaim(characterGroup, playerUserId: null).Any();
      }

      public static IReadOnlyCollection<AddClaimForbideReason> ValidateIfCanAddClaim<T>(this T claimSource,
          int? playerUserId)
      where T: IClaimSource
      {
          IEnumerable<AddClaimForbideReason> ValidateImpl()
          {
              var project = claimSource.Project;

              if (!project.Active)
              {
                  yield return AddClaimForbideReason.ProjectNotActive;
              }

              if (!project.IsAcceptingClaims)
              {
                  yield return AddClaimForbideReason.ProjectClaimsClosed;
              }

              switch (claimSource)
              {
                  case CharacterGroup characterGroup:
                      if (!characterGroup.HaveDirectSlots)
                      {
                          yield return AddClaimForbideReason.NotForDirectClaims;
                      }
                      else if (!characterGroup.DirectSlotsUnlimited &&
                               characterGroup.AvaiableDirectSlots == 0)
                      {
                          yield return AddClaimForbideReason.SlotsExhausted;
                      }

                      break;
                  case Character character:
                      if (character.ApprovedClaimId != null)
                      {
                          yield return AddClaimForbideReason.Busy;
                      }

                      if (!character.IsAcceptingClaims)
                      {
                          yield return AddClaimForbideReason.Npc;
                      }

                      break;
              }

              if (playerUserId is int playerId)
              {
                  if (claimSource.HasClaimForUser(playerId))
                  {
                      if (!(claimSource is CharacterGroup) ||
                          !project.Details.EnableManyCharacters)
                      {
                          yield return AddClaimForbideReason.AlreadySent;
                      }
                  }

                  if (!project.Details.EnableManyCharacters && project.Claims.OfUserActive(playerId).Any())
                  {
                      yield return AddClaimForbideReason.OnlyOneCharacter;
                  }
              }
          }

          return ValidateImpl().ToList();
      }

      internal static void ThrowForReason(AddClaimForbideReason reason)
      {
          switch (reason)
          {
              case AddClaimForbideReason.ProjectNotActive:
                  throw new ProjectDeactivedException();
              case AddClaimForbideReason.ProjectClaimsClosed:
              case AddClaimForbideReason.SlotsExhausted:
              case AddClaimForbideReason.NotForDirectClaims:
              case AddClaimForbideReason.Busy:
              case AddClaimForbideReason.Npc:
                  throw new ClaimTargetIsNotAcceptingClaims();
              case AddClaimForbideReason.AlreadySent:
                  throw new ClaimAlreadyPresentException();
              case AddClaimForbideReason.OnlyOneCharacter:
                  throw new OnlyOneApprovedClaimException();
              default:
                  throw new ArgumentOutOfRangeException(nameof(reason), reason, message: null);
          }
      }

      public static void EnsureCanAddClaim<T>(this T claimSource, int currentUserId)
          where T : IClaimSource
      {
          foreach (var addClaimForbideReason in claimSource.ValidateIfCanAddClaim(currentUserId))
          {
              ThrowForReason(addClaimForbideReason);
          }
      }
  }

    public enum AddClaimForbideReason
    {
        ProjectNotActive,
        ProjectClaimsClosed,
        NotForDirectClaims,
        SlotsExhausted,
        Npc,
        Busy,
        AlreadySent,
        OnlyOneCharacter,
    }
}
