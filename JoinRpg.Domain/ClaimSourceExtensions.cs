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
        [NotNull]
        public static IEnumerable<CharacterGroup> GetParentGroupsToTop([CanBeNull]
            this IClaimSource target)
        {
            return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                       .OrderBy(g => g.CharacterGroupId)
                       .Distinct() ?? Enumerable.Empty<CharacterGroup>();
        }

        [NotNull, ItemNotNull]
        public static IEnumerable<CharacterGroup> GetChildrenGroups([NotNull]
            this CharacterGroup target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups)).Distinct();
        }

        public static bool HasActiveClaims(this IClaimSource target)
        {
            return target.Claims.Any(claim => claim.ClaimStatus.IsActive());
        }

        public static bool IsNpc([CanBeNull]
            this IClaimSource target)
        {
            return target is Character character && !character.IsAcceptingClaims &&
                   character.ApprovedClaim == null;
        }

        public static bool IsAcceptingClaims<T>(this T characterGroup)
            where T : IClaimSource
        {
            return !ValidateIfCanAddClaim(characterGroup, playerUserId: null).Any();
        }

        public static IReadOnlyCollection<AddClaimForbideReason> ValidateIfCanAddClaim<T>(
            this T claimSource,
            int? playerUserId)
            where T : IClaimSource
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
                    if (claimSource.Claims.OfUserActive(playerId).Any())
                    {
                        if (!(claimSource is CharacterGroup) ||
                            !project.Details.EnableManyCharacters)
                        {
                            yield return AddClaimForbideReason.AlreadySent;
                        }
                    }

                    if (!project.Details.EnableManyCharacters &&
                        project.Claims.OfUserApproved(playerId).Any())
                    {
                        yield return AddClaimForbideReason.OnlyOneCharacter;
                    }
                }
            }

            return ValidateImpl().ToList();
        }


        public static IReadOnlyCollection<AddClaimForbideReason> ValidateIfCanMoveClaim(this IClaimSource claimSource, Claim claim)
        {
            return claimSource.ValidateIfCanAddClaim(claim.PlayerUserId);
        }

        public static void EnsureCanAddClaim<T>(this T claimSource, int currentUserId)
            where T : IClaimSource
        {
            ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(currentUserId));
        }

        public static void EnsureCanMoveClaim(this IClaimSource claimSource, Claim claim)
        {
            ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim));
        }

        private static void ThrowIfValidationFailed(
            IReadOnlyCollection<AddClaimForbideReason> validation)
        {
            if (validation.Any())
            {
                ThrowForReason(validation.First());
            }
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
                case AddClaimForbideReason.ApprovedClaimMovedToGroup:
                    throw new ApprovedClaimCannotBeMovedFromCharacterException();
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, message: null);
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
        ApprovedClaimMovedToGroup,
    }
}
