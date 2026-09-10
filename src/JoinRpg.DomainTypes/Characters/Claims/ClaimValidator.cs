using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.DomainTypes.Users;

namespace JoinRpg.DomainTypes.Characters.Claims;

/// <summary>
/// Правила, по которым заявку можно или нельзя создать на персонажа либо перенести на него.
/// </summary>
/// <remarks>
/// Трансляция причин в исключения живёт снаружи (<c>JoinRpg.Domain</c>): ей нужна EF-сущность
/// заявки, а сюда EF не заходит.
/// </remarks>
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
    /// <remarks>
    /// <para>
    /// Сначала выбрасываются причины, которые мастер вправе обойти, и только потом применяется
    /// фатальность. Порядок принципиален: если сделать наоборот, мастер, приглашающий игрока в
    /// проект с закрытым приёмом заявок, увидел бы пустой список — фатальный
    /// <see cref="AddClaimForbideReason.ProjectClaimsClosed"/> вытеснил бы
    /// <see cref="AddClaimForbideReason.Busy"/> и <see cref="AddClaimForbideReason.Npc"/>, а потом
    /// ушёл бы сам, и вместе с ним молча ушли бы все остальные проверки.
    /// </para>
    /// <para>
    /// Фатальная причина (проект в архиве, приём заявок закрыт) вытесняет остальные: пока проект
    /// в таком состоянии, разбираться с занятостью роли или контактами игрока бессмысленно.
    /// </para>
    /// </remarks>
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

        var reasons = ValidateImpl(target, userInfo, movedClaim, projectInfo, operation)
            .Select(ClaimForbiddenReason.For)
            .Where(reason => !(byMaster && reason.MasterCanOverride))
            .ToList();

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
}
