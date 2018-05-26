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
            return ValidateImpl(claimSource, playerUserId, existingClaim: null).ToList();
        }


        public static IReadOnlyCollection<AddClaimForbideReason> ValidateIfCanMoveClaim(this IClaimSource claimSource, Claim claim)
        {
            return ValidateImpl(claimSource, claim.PlayerUserId, claim).ToList();
        }

        public static void EnsureCanAddClaim<T>(this T claimSource, int currentUserId)
            where T : IClaimSource
        {
            ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(currentUserId), claim: null);
        }

        public static void EnsureCanMoveClaim(this IClaimSource claimSource, Claim claim)
        {
            ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim), claim);
        }

        private static void ThrowIfValidationFailed(
            IReadOnlyCollection<AddClaimForbideReason> validation,
            Claim claim)
        {
            if (validation.Any())
            {
                ThrowForReason(validation.First(), claim);
            }
        }

        internal static void ThrowForReason(AddClaimForbideReason reason, Claim claim)
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
                case AddClaimForbideReason.CheckedInClaimCantBeMoved:
                    throw new ClaimWrongStatusException(claim);
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, message: null);
            }
        }

        /// <summary>
        /// Perform real validation
        /// </summary>
        /// <param name="claimSource">Where we are trying to add/move claim</param>
        /// <param name="playerUserId">User</param>
        /// <param name="existingClaim">If we already have claim (move), that's it</param>
        /// <returns></returns>
        private static IEnumerable<AddClaimForbideReason> ValidateImpl(this IClaimSource claimSource, int? playerUserId, [CanBeNull] Claim existingClaim)
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

                    if (existingClaim?.IsApproved == true)
                    {
                        yield return AddClaimForbideReason.ApprovedClaimMovedToGroup;
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

            if (existingClaim?.ClaimStatus == Claim.Status.CheckedIn)
            {
                yield return AddClaimForbideReason.CheckedInClaimCantBeMoved;
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
                    project.Claims.OfUserApproved(playerId).Except(new [] {existingClaim}).Any())
                {
                    yield return AddClaimForbideReason.OnlyOneCharacter;
                }
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
        CheckedInClaimCantBeMoved,
    }
}
