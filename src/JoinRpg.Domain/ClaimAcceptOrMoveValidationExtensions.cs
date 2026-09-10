using System.Diagnostics.CodeAnalysis;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.Domain;

public static class ClaimAcceptOrMoveValidationExtensions
{
    public static bool IsAcceptingClaims(this Character character, ProjectInfo projectInfo)
        => !ValidateIfCanAddClaim(character, userInfo: null, projectInfo, ClaimOperation.DisplayForPlayer).Any();

    /// <summary>
    /// Причины, по которым нельзя создать заявку на этого персонажа.
    /// </summary>
    /// <param name="userInfo">
    /// Игрок, на которого оформляется заявка; <c>null</c> — если он неизвестен (тогда правила про
    /// контакты и уже поданные заявки не считаются).
    /// </param>
    /// <param name="operation">
    /// Для <see cref="ClaimOperation.DisplayForPlayer"/> мастерских послаблений нет: страница
    /// показывает положение дел глазами игрока, кто бы её ни открыл.
    /// </param>
    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanAddClaim(
        this Character claimSource,
        UserInfo? userInfo, ProjectInfo projectInfo, ClaimOperation operation)
        => Validate(claimSource, userInfo, existingClaim: null, projectInfo, operation);

    public static IReadOnlyCollection<ClaimForbiddenReason> ValidateIfCanMoveClaim(this Character claimSource, Claim claim, UserInfo userInfo, ProjectInfo projectInfo)
        => Validate(claimSource, userInfo, claim, projectInfo, ClaimOperation.MoveByMaster);

    /// <param name="userInfo">
    /// Игрок, на которого оформляется заявка. При <see cref="ClaimOperation.AddByMaster"/> это не
    /// тот, кто выполняет операцию: правила про контакты и уже поданные заявки относятся к игроку.
    /// </param>
    public static void EnsureCanAddClaim([NotNull] this Character claimSource, UserInfo userInfo, ProjectInfo projectInfo, ClaimOperation operation)
    {
        ArgumentNullException.ThrowIfNull(claimSource);
        ThrowIfValidationFailed(
            claimSource.ValidateIfCanAddClaim(userInfo, projectInfo, operation),
            claim: null,
            projectInfo);
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
    /// Считает все причины запрета и отбрасывает те, которые не мешают этой операции.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Сначала выбрасываются причины, которые мастер вправе обойти, и только потом применяется
    /// фатальность. Порядок принципиален: если сделать наоборот, мастер, приглашающий игрока в
    /// проект с закрытым приёмом заявок, увидел бы пустой список — фатальный
    /// <c>ProjectClaimsClosed</c> вытеснил бы <c>Busy</c> и <c>Npc</c>, а потом ушёл бы сам,
    /// и вместе с ним молча ушли бы все остальные проверки.
    /// </para>
    /// <para>
    /// Фатальная причина (проект в архиве, приём заявок закрыт) вытесняет остальные: пока проект
    /// в таком состоянии, разбираться с занятостью роли или контактами игрока бессмысленно.
    /// </para>
    /// </remarks>
    private static List<ClaimForbiddenReason> Validate(
        Character character, UserInfo? userInfo, Claim? existingClaim, ProjectInfo projectInfo, ClaimOperation operation)
    {
        var byMaster = operation.PerformedByMaster();

        var reasons = ValidateImpl(character, userInfo, existingClaim, projectInfo, operation)
            .Select(ClaimForbiddenReason.For)
            .Where(r => !(byMaster && r.MasterCanOverride))
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
    /// <param name="operation">Операция, ради которой считаются правила.</param>
    private static IEnumerable<AddClaimForbideReason> ValidateImpl(this Character character, UserInfo? playerUserId, Claim? existingClaim, ProjectInfo projectInfo, ClaimOperation operation)
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

        if (operation.IsMove())
        {
            if (existingClaim?.IsApproved == true && character.CharacterType == CharacterType.Slot)
            {
                yield return AddClaimForbideReason.ApprovedClaimMovedToGroupOrSlot;
            }

            if (existingClaim?.ClaimStatus == ClaimStatus.CheckedIn)
            {
                yield return AddClaimForbideReason.CheckedInClaimCantBeMoved;
            }
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
        var problems = UserProfileProblemsCalculator.GetProblems(userInfo, projectInfo.ProfileRequirementSettings);

        // Заявку блокируют только обязательные (Required => Warning) требования — Recommended (Hint) не мешает подать заявку.
        foreach (var problem in problems.Where(p => p.Severity == ProblemSeverity.Warning))
        {
            yield return ToAddClaimForbideReason(problem.ItemType);
        }
    }

    internal static AddClaimForbideReason ToAddClaimForbideReason(UserProfileItemType itemType) => itemType switch
    {
        UserProfileItemType.Telegram => AddClaimForbideReason.TelegramMissing,
        UserProfileItemType.Vkontakte => AddClaimForbideReason.VkontakteMissing,
        UserProfileItemType.Phone => AddClaimForbideReason.PhoneMissing,
        UserProfileItemType.RealName => AddClaimForbideReason.RealNameMissing,
        _ => throw new ArgumentOutOfRangeException(nameof(itemType)),
    };

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
