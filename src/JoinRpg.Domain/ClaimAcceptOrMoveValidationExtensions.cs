using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

public static class ClaimAcceptOrMoveValidationExtensions
{
    public static bool IsAcceptingClaims(this Character character, ProjectInfo projectInfo)
        => !ValidateIfCanAddClaim(character, userInfo: null, projectInfo).Any();

    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanAddClaim(
        this Character claimSource,
        UserInfo? userInfo, ProjectInfo projectInfo)
        => Validate(claimSource, userInfo, existingClaim: null, projectInfo);

    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanMoveClaim(this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
        => Validate(claimSource, userInfo, claim, projectInfo);

    public static void EnsureCanAddClaim([NotNull] this Character claimSource, UserInfo userInfo, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanAddClaim(userInfo, projectInfo), claim: null, projectInfo);
    }

    public static void EnsureCanMoveClaim([NotNull] this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(claimSource.ValidateIfCanMoveClaim(claim, userInfo, projectInfo), claim, projectInfo);
    }

    private static void ThrowIfValidationFailed(
        IReadOnlyCollection<ClaimForbiddenReason> validation,
        Claim? claim,
        ProjectInfo projectInfo)
    {
        if (validation.Count > 0)
        {
            ThrowForReason(validation.First(), claim, projectInfo);
        }
    }

    internal static void ThrowForReason(ClaimForbiddenReason reason, Claim? claim, ProjectInfo projectInfo)
    {
        throw reason.Kind switch
        {
            AddClaimForbideReason.ProjectNotActive => new ProjectDeactivatedException(projectInfo.ProjectId),

            AddClaimForbideReason.ProjectClaimsClosed or AddClaimForbideReason.SlotsExhausted
                or AddClaimForbideReason.Busy or AddClaimForbideReason.Npc or AddClaimForbideReason.CharacterInactive
                    => new ClaimTargetIsNotAcceptingClaims(),

            AddClaimForbideReason.AlreadySent => new ClaimAlreadyPresentException(),
            AddClaimForbideReason.OnlyOneCharacter => new OnlyOneApprovedClaimException(),

            AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot or AddClaimForbideReason.CheckedInClaimCantBeMoved => new ClaimWrongStatusException(claim!),
            AddClaimForbideReason.RealNameMissing or AddClaimForbideReason.PhoneMissing or
            AddClaimForbideReason.TelegramMissing or AddClaimForbideReason.VkontakteMissing => new InsufficientContactsException(),
            _ => new ArgumentOutOfRangeException(nameof(reason), reason.Kind, message: null),
        };
    }

    /// <summary>
    /// Считает все причины запрета и отбрасывает те, которые не надо показывать.
    /// </summary>
    /// <remarks>
    /// Если есть хотя бы одна фатальная причина (проект в архиве, приём заявок закрыт), остальные
    /// не отдаём: пока проект в таком состоянии, разбираться с занятостью роли или контактами
    /// игрока бессмысленно.
    /// </remarks>
    private static List<ClaimForbiddenReason> Validate(
        Character character, UserInfo? userInfo, Claim? existingClaim, ProjectInfo projectInfo)
    {
        var reasons = ValidateImpl(character, userInfo, existingClaim, projectInfo)
            .Select(ClaimForbiddenReason.For)
            .ToList();

        var fatal = reasons.Where(r => r.IsFatal).ToList();
        return fatal.Count > 0 ? fatal : reasons;
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
            if (projectInfo.ClaimSettings.StrictlyOneCharacter &&
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
        if (projectInfo.ProfileRequirementSettings.RequireVkontakte == MandatoryStatus.Required && userInfo.Social.Vk?.IsVerified != true)
        {
            yield return AddClaimForbideReason.VkontakteMissing;
        }
        if (projectInfo.ProfileRequirementSettings.RequireTelegram == MandatoryStatus.Required && userInfo.Social.Telegram is null)
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
