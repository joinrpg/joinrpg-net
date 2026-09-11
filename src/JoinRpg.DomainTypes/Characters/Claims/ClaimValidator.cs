using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Правила, по которым заявку можно или нельзя создать на персонажа либо перенести на него.
/// </summary>
public static class ClaimValidator
{
    /// <summary>
    /// Считает все причины запрета и отбрасывает те, которые не мешают этой операции.
    /// </summary>
    /// <param name="userInfo">
    /// Игрок, на которого оформляется заявка. При мастерских операциях это не тот, кто выполняет
    /// операцию: правила про контакты и уже поданные заявки относятся к игроку. <c>null</c> —
    /// если игрок неизвестен (например при построении списка доступных персонажей); тогда правила
    /// про игрока не считаются.
    /// </param>
    /// <param name="movedClaim">Переносимая заявка — только для операций переноса.</param>
    public static IReadOnlyCollection<ClaimForbiddenReason> Validate(
        IClaimTarget target,
        UserInfo? userInfo,
        UserClaimInfo? movedClaim,
        ProjectInfo projectInfo,
        ClaimOperation operation)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(projectInfo);

        var byMaster = operation.PerformedByMaster();

        // Обходимые мастером причины выбрасываются ДО фильтра по фатальности. Наоборот нельзя:
        // фатальный ProjectClaimsClosed сначала вытеснил бы Busy и Npc, а потом ушёл бы сам как
        // обходимый — и мастер смог бы пригласить игрока на занятую роль.
        var reasons = ValidateImpl(target, userInfo, movedClaim, projectInfo, operation)
            .Select(ClaimForbiddenReason.For)
            .Where(reason => !(byMaster && reason.MasterCanOverride))
            .ToList();

        // Пока проект в архиве или не принимает заявки, разбираться с занятостью роли и
        // контактами игрока бессмысленно — показываем только фатальное.
        var fatal = reasons.Where(reason => reason.IsFatal).ToList();
        return fatal.Count > 0 ? fatal : reasons;
    }

    private static IEnumerable<AddClaimForbideReason> ValidateImpl(
        IClaimTarget target,
        UserInfo? userInfo,
        UserClaimInfo? movedClaim,
        ProjectInfo projectInfo,
        ClaimOperation operation)
    {
        if (ValidateProject(projectInfo) is AddClaimForbideReason projectReason)
        {
            yield return projectReason;
        }

        if (target.ApprovedClaimId is not null)
        {
            yield return AddClaimForbideReason.Busy;
        }

        if (!target.IsActive)
        {
            yield return AddClaimForbideReason.CharacterInactive;
        }

        var characterType = target.CharacterTypeInfo.CharacterType;

        switch (characterType)
        {
            case CharacterType.Player:
                break;
            case CharacterType.NonPlayer:
                yield return AddClaimForbideReason.Npc;
                break;
            case CharacterType.Slot:
                if (target.CharacterTypeInfo.SlotLimit == 0)
                {
                    yield return AddClaimForbideReason.SlotsExhausted;
                }
                break;
        }

        if (operation.IsMove())
        {
            if (movedClaim?.IsApproved == true && characterType == CharacterType.Slot)
            {
                yield return AddClaimForbideReason.ApprovedClaimMovedToSlot;
            }

            if (movedClaim?.Status == ClaimStatus.CheckedIn)
            {
                yield return AddClaimForbideReason.CheckedInClaimCantBeMoved;
            }
        }

        if (userInfo is not null)
        {
            if (target.HasActiveClaimOf(userInfo.UserId))
            {
                yield return AddClaimForbideReason.AlreadySent;
            }

            if (projectInfo.ClaimSettings.StrictlyOneCharacter
                && HasOtherApprovedClaim(userInfo, projectInfo, movedClaim))
            {
                yield return AddClaimForbideReason.OnlyOneCharacter;
            }

            foreach (var reason in ValidateContacts(projectInfo, userInfo))
            {
                yield return reason;
            }
        }
    }

    /// <summary>
    /// У игрока уже есть утверждённая заявка в этом проекте — не считая той, которую переносим.
    /// </summary>
    private static bool HasOtherApprovedClaim(UserInfo userInfo, ProjectInfo projectInfo, UserClaimInfo? movedClaim)
        => userInfo.ActiveClaims.Any(claim =>
            claim.ProjectId == projectInfo.ProjectId
            && claim.IsApproved
            && claim.ClaimId != movedClaim?.ClaimId);

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

    private static AddClaimForbideReason? ValidateProject(ProjectInfo project)
        => project.ProjectStatus switch
        {
            ProjectLifecycleStatus.ActiveClaimsOpen => null,
            ProjectLifecycleStatus.Archived => AddClaimForbideReason.ProjectNotActive,
            ProjectLifecycleStatus.ActiveClaimsClosed => AddClaimForbideReason.ProjectClaimsClosed,
            _ => throw new NotImplementedException(),
        };

    /// <summary>
    /// Бросает исключение, соответствующее причине запрета.
    /// </summary>
    /// <param name="claim">
    /// Переносимая заявка — нужна только для <see cref="ClaimWrongStatusException"/>.
    /// </param>
    public static void ThrowForReason(ClaimForbiddenReason reason, UserClaimInfo? claim, ProjectInfo projectInfo)
    {
        throw reason.Kind switch
        {
            AddClaimForbideReason.ProjectNotActive => new ProjectDeactivatedException(projectInfo.ProjectId),

            AddClaimForbideReason.ProjectClaimsClosed or AddClaimForbideReason.SlotsExhausted
                or AddClaimForbideReason.Busy or AddClaimForbideReason.Npc or AddClaimForbideReason.CharacterInactive
                    => new ClaimTargetIsNotAcceptingClaims(),

            AddClaimForbideReason.AlreadySent => new ClaimAlreadyPresentException(),
            AddClaimForbideReason.OnlyOneCharacter => new OnlyOneApprovedClaimException(),

            AddClaimForbideReason.ApprovedClaimMovedToSlot or AddClaimForbideReason.CheckedInClaimCantBeMoved
                => new ClaimWrongStatusException(claim!.ClaimId, claim.Status),

            AddClaimForbideReason.RealNameMissing or AddClaimForbideReason.PhoneMissing or
            AddClaimForbideReason.TelegramMissing or AddClaimForbideReason.VkontakteMissing => new InsufficientContactsException(),

            _ => new ArgumentOutOfRangeException(nameof(reason), reason.Kind, message: null),
        };
    }
}
