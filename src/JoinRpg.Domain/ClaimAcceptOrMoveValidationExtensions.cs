using System.Diagnostics.CodeAnalysis;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Domain;

public static class ClaimAcceptOrMoveValidationExtensions
{
    public static bool IsAcceptingClaims(this Character character)
        => !ValidateIfCanAddClaim(character, playerUserId: null).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanAddClaim(
        this Character claimSource,
        int? playerUserId)
        => ValidateImpl(claimSource, playerUserId, existingClaim: null).ToList();

    public static bool CanMoveClaimTo(this Character character, Claim claim) => !ValidateIfCanMoveClaim(character, claim).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanMoveClaim(this Character claimSource, Claim claim)
        => ValidateImpl(claimSource, claim.PlayerUserId, claim);

    public static void EnsureCanAddClaim([NotNull] this Character claimSource, int currentUserId)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(currentUserId), claim: null);
    }

    public static void EnsureCanMoveClaim([NotNull] this Character claimSource, Claim claim)
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

            AddClaimForbideReason.ProjectClaimsClosed or AddClaimForbideReason.SlotsExhausted
                or AddClaimForbideReason.Busy or AddClaimForbideReason.Npc or AddClaimForbideReason.CharacterInactive
                    => new ClaimTargetIsNotAcceptingClaims(),

            AddClaimForbideReason.AlreadySent => new ClaimAlreadyPresentException(),
            AddClaimForbideReason.OnlyOneCharacter => new OnlyOneApprovedClaimException(),

            AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot or AddClaimForbideReason.CheckedInClaimCantBeMoved => new ClaimWrongStatusException(claim!),
            _ => new ArgumentOutOfRangeException(nameof(reason), reason, message: null),
        };
    }

    /// <summary>
    /// Perform real validation
    /// </summary>
    /// <param name="character">Where we are trying to add/move claim</param>
    /// <param name="playerUserId">User</param>
    /// <param name="existingClaim">If we already have claim (move), that's it</param>
    /// <returns></returns>
    private static IEnumerable<AddClaimForbideReason> ValidateImpl(this Character character, int? playerUserId, Claim? existingClaim)
    {
        var project = character.Project;

        if (!project.Active)
        {
            yield return AddClaimForbideReason.ProjectNotActive;
        }

        if (!project.IsAcceptingClaims)
        {
            yield return AddClaimForbideReason.ProjectClaimsClosed;
        }


        if (character.ApprovedClaimId != null)
        {
            yield return AddClaimForbideReason.Busy;
        }

        if (!character.IsActive)
        {
            yield return AddClaimForbideReason.CharacterInactive;
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


        if (existingClaim?.ClaimStatus == Claim.Status.CheckedIn)
        {
            yield return AddClaimForbideReason.CheckedInClaimCantBeMoved;
        }

        if (playerUserId is int playerId)
        {
            if (character.Claims.OfUserActive(playerId).Any())
            {
                yield return AddClaimForbideReason.AlreadySent;
            }

            if (!project.Details.EnableManyCharacters &&
                project.Claims.OfUserApproved(playerId).Except([existingClaim]).Any())
            {
                yield return AddClaimForbideReason.OnlyOneCharacter;
            }
        }
    }

}
