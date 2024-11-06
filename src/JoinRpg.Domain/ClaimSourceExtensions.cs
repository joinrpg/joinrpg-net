using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

public static class ClaimSourceExtensions
{
    public static IEnumerable<CharacterGroup> GetParentGroupsToTop(this IClaimSource? target)
    {
        return target?.ParentGroups.SelectMany(g => g.FlatTree(gr => gr.ParentGroups))
                   .OrderBy(g => g.CharacterGroupId)
                   .Distinct() ?? Enumerable.Empty<CharacterGroup>();
    }

    public static IEnumerable<CharacterGroup> GetChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.ChildGroups)).Distinct();
    }

    public static IEnumerable<CharacterGroup> GetOrderedChildrenGroupsRecursive(
        this CharacterGroup target)
    {
        ArgumentNullException.ThrowIfNull(target);

        return target.ChildGroups.SelectMany(g => g.FlatTree(gr => gr.GetOrderedChildGroups())).Distinct();
    }

    public static bool HasActiveClaims(this IClaimSource target) => target.Claims.Any(claim => claim.ClaimStatus.IsActive());

    public static bool IsAcceptingClaims<T>(this T characterGroup)
        where T : IClaimSource => !ValidateIfCanAddClaim(characterGroup, playerUserId: null).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanAddClaim<T>(
        this T claimSource,
        int? playerUserId)
        where T : IClaimSource => ValidateImpl(claimSource, playerUserId, existingClaim: null).ToList();

    public static bool CanMoveClaimTo(this Character character, Claim claim) => !ValidateIfCanMoveClaim(character, claim).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanMoveClaim(this IClaimSource claimSource, Claim claim)
        => ValidateImpl(claimSource, claim.PlayerUserId, claim);

    public static void EnsureCanAddClaim<T>([NotNull] this T? claimSource, int currentUserId)
        where T : IClaimSource
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(currentUserId), claim: null);
    }

    public static void EnsureCanMoveClaim([NotNull] this IClaimSource? claimSource, Claim claim)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim), claim);
    }

    private static void ThrowIfValidationFailed(
        IEnumerable<AddClaimForbideReason> validation,
        Claim? claim)
    {
        if (validation.Any())
        {
            ThrowForReason(validation.First(), claim);
        }
    }

    internal static void ThrowForReason(AddClaimForbideReason reason, Claim? claim)
    {
        throw reason switch
        {
            AddClaimForbideReason.ProjectNotActive => new ProjectDeactivatedException(),

            AddClaimForbideReason.ProjectClaimsClosed or AddClaimForbideReason.SlotsExhausted or
            AddClaimForbideReason.NotForDirectClaims or AddClaimForbideReason.Busy or AddClaimForbideReason.Npc => new ClaimTargetIsNotAcceptingClaims(),

            AddClaimForbideReason.AlreadySent => new ClaimAlreadyPresentException(),
            AddClaimForbideReason.OnlyOneCharacter => new OnlyOneApprovedClaimException(),

            AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot or AddClaimForbideReason.CheckedInClaimCantBeMoved => new ClaimWrongStatusException(claim!),
            _ => new ArgumentOutOfRangeException(nameof(reason), reason, message: null),
        };
    }

    /// <summary>
    /// Perform real validation
    /// </summary>
    /// <param name="claimSource">Where we are trying to add/move claim</param>
    /// <param name="playerUserId">User</param>
    /// <param name="existingClaim">If we already have claim (move), that's it</param>
    /// <returns></returns>
    private static IEnumerable<AddClaimForbideReason> ValidateImpl(this IClaimSource claimSource, int? playerUserId, Claim? existingClaim)
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
                    yield return AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot;
                }

                break;
            case Character character:
                if (character.ApprovedClaimId != null)
                {
                    yield return AddClaimForbideReason.Busy;
                }

                switch (character.CharacterType)
                {
                    case CharacterType.Player:
                        break;
                    case CharacterType.NonPlayer:
                        yield return AddClaimForbideReason.Npc;
                        break;
                    case CharacterType.Slot:
                        if (character.CharacterSlotLimit == 0)
                        {
                            yield return AddClaimForbideReason.SlotsExhausted;
                        }

                        if (existingClaim?.IsApproved == true)
                        {
                            yield return AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot;
                        }
                        break;
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
                project.Claims.OfUserApproved(playerId).Except(new[] { existingClaim }).Any())
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
    ApprovedClaimMovedToGroupOrSlot,
    CheckedInClaimCantBeMoved,
}
