using System.Diagnostics.CodeAnalysis;
using JoinRpg.PrimitiveTypes.Claims;
using JoinRpg.PrimitiveTypes.Users;

namespace JoinRpg.Domain;

public static class ClaimAcceptOrMoveValidationExtensions
{
    public static bool IsAcceptingClaims(this Character character, ProjectInfo projectInfo)
        => !ValidateIfCanAddClaim(character, userInfo: null, projectInfo).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanAddClaim(
        this Character claimSource,
        UserInfo? userInfo, ProjectInfo projectInfo)
        => ValidateImpl(claimSource, userInfo, existingClaim: null, projectInfo).ToList();

    public static bool CanMoveClaimTo(this Character character, Claim claim, UserInfo userInfo, ProjectInfo projectInfo) => !ValidateIfCanMoveClaim(character, claim, userInfo, projectInfo).Any();

    public static IEnumerable<AddClaimForbideReason> ValidateIfCanMoveClaim(this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
        => ValidateImpl(claimSource, userInfo, claim, projectInfo);

    public static void EnsureCanAddClaim([NotNull] this Character claimSource, UserInfo userInfo, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(userInfo, projectInfo), claim: null);
    }

    public static void EnsureCanMoveClaim([NotNull] this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim, userInfo, projectInfo), claim);
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
            AddClaimForbideReason.RealNameMissing or AddClaimForbideReason.PhoneMissing or
            AddClaimForbideReason.TelegramMissing or AddClaimForbideReason.VkontakteMissing => throw new InsufficientContactsException(),
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
    /// <param name="projectInfo"></param>
    private static IEnumerable<AddClaimForbideReason> ValidateImpl(this Character character, UserInfo? playerUserId, Claim? existingClaim, ProjectInfo projectInfo)
    {
        var project = character.Project;

        if (ValidateProjectImpl(projectInfo) is AddClaimForbideReason projectReason)
        {
            yield return projectReason;
            yield break;
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


                break;
        }

        if (existingClaim?.IsApproved == true && character.CharacterType == CharacterType.Slot)
        {
            yield return AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot;
        }


        if (existingClaim?.ClaimStatus == ClaimStatus.CheckedIn)
        {
            yield return AddClaimForbideReason.CheckedInClaimCantBeMoved;
        }

        if (playerUserId is UserInfo userInfo)
        {
            if (character.Claims.OfUserActive(userInfo.UserId.Value).Any())
            {
                yield return AddClaimForbideReason.AlreadySent;
            }

            // TODO вот здесь бы проверять по UserInfo
            if (!projectInfo.AllowManyClaims &&
                project.Claims.OfUserApproved(userInfo.UserId.Value).Except([existingClaim]).Any())
            {
                yield return AddClaimForbideReason.OnlyOneCharacter;
            }

            foreach (var r in ValidateContacts(projectInfo, userInfo))
            {
                yield return r;
            }
        }
    }

    private static IEnumerable<AddClaimForbideReason> ValidateContacts(ProjectInfo projectInfo, UserInfo userInfo)
    {
        if (projectInfo.ProfileRequirementSettings.RequireVkontakte == MandatoryStatus.Required && userInfo.Social.VkId is null)
        {
            yield return AddClaimForbideReason.VkontakteMissing;
        }
        if (projectInfo.ProfileRequirementSettings.RequireTelegram == MandatoryStatus.Required && userInfo.Social.TelegramId is null)
        {
            yield return AddClaimForbideReason.TelegramMissing;
        }
        if (projectInfo.ProfileRequirementSettings.RequireRealName == MandatoryStatus.Required && userInfo.UserFullName.FullName?.Length < 5)
        {
            yield return AddClaimForbideReason.RealNameMissing;
        }
        if (projectInfo.ProfileRequirementSettings.RequirePhone == MandatoryStatus.Required && userInfo.PhoneNumber?.Length < 5)
        {
            yield return AddClaimForbideReason.PhoneMissing;
        }
    }

    private static AddClaimForbideReason? ValidateProjectImpl(ProjectInfo project)
    {
        return project.ProjectStatus switch
        {
            ProjectLifecycleStatus.ActiveClaimsOpen => null,
            ProjectLifecycleStatus.Archived => AddClaimForbideReason.ProjectNotActive,
            ProjectLifecycleStatus.ActiveClaimsClosed => AddClaimForbideReason.ProjectClaimsClosed,
            _ => throw new NotImplementedException(),
        };
    }
}
