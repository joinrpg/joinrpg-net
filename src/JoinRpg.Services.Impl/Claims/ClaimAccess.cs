using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Impl.Claims;

/// <summary>
/// Разворачивает <see cref="ClaimAccessRequirement"/> в проверку доступа (ADR014).
/// </summary>
/// <remarks>
/// Проверка идёт по доменным снимкам — <see cref="ProjectInfo"/> и
/// <see cref="CharacterClaimInfo"/>, — а не по EF-графу, как legacy <c>Claim.RequestAccess</c>,
/// который читал <c>claim.Project.ProjectAcls</c> через <c>[Obsolete]</c>-перегрузку. Данные те же,
/// правила перенесены один в один.
/// </remarks>
internal static class ClaimAccess
{
    /// <summary>
    /// Проверка доступа к <b>существующей</b> заявке.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Передан <see cref="ClaimAccessRequirement.NoCheck"/>: «проверок нет» осмысленно только при
    /// создании заявки игроком, а над существующей заявкой означало бы доступ кому угодно.
    /// </exception>
    public static void Request(
        ProjectInfo projectInfo,
        CharacterClaimInfo claimInfo,
        ICurrentUserAccessor currentUser,
        ClaimAccessRequirement requirement)
    {
        if (requirement == ClaimAccessRequirement.NoCheck)
        {
            throw new ArgumentOutOfRangeException(
                nameof(requirement),
                requirement,
                "Операция над существующей заявкой обязана назвать требование доступа");
        }

        var userId = currentUser.UserIdentificationOrDefault;

        if (requirement == ClaimAccessRequirement.PlayerOnly)
        {
            if (userId is null || claimInfo.PlayerId != userId)
            {
                throw new PlayerOnlyException(claimInfo.ClaimId, currentUser.UserIdOrDefault ?? 0);
            }

            return;
        }

        var (permission, reason) = Resolve(requirement, claimInfo);

        if (userId is null)
        {
            throw new NoAccessToProjectException(projectInfo, null);
        }

        if (reason.HasFlag(ExtraAccessReason.Player) && claimInfo.PlayerId == userId)
        {
            return;
        }

        if (reason.HasFlag(ExtraAccessReason.ResponsibleMaster) && claimInfo.ResponsibleMasterId == userId)
        {
            return;
        }

        // Admin-bypass'а здесь нет и не должно быть: в claim-путях его никогда не было, и добавление
        // выдало бы администраторам доступ ко всем заявкам всех проектов (ADR014).
        _ = projectInfo.RequestMasterAccess(userId, permission);
    }

    /// <summary>
    /// Проверка доступа при <b>создании</b> заявки: заявки ещё нет, поэтому послабления «я игрок»
    /// и «я ответственный мастер» опереться не на что — остаются только права в проекте.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Передано требование, которому нужна уже существующая заявка.
    /// </exception>
    public static void RequestForCreation(
        ProjectInfo projectInfo,
        ICurrentUserAccessor currentUser,
        ClaimAccessRequirement requirement)
    {
        switch (requirement)
        {
            case ClaimAccessRequirement.NoCheck:
                break;
            case ClaimAccessRequirement.AnyMaster:
                _ = projectInfo.RequestMasterAccess(currentUser);
                break;
            case ClaimAccessRequirement.ManageClaims:
                _ = projectInfo.RequestMasterAccess(currentUser, Permission.CanManageClaims);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(requirement),
                    requirement,
                    "Этому требованию нужна существующая заявка, а при создании её ещё нет");
        }
    }

    private static (Permission Permission, ExtraAccessReason Reason) Resolve(
        ClaimAccessRequirement requirement,
        CharacterClaimInfo claimInfo)
        => requirement switch
        {
            ClaimAccessRequirement.MasterOrPlayer => (Permission.None, ExtraAccessReason.Player),
            ClaimAccessRequirement.AnyMaster => (Permission.None, ExtraAccessReason.None),
            ClaimAccessRequirement.ApprovalDecline => (Permission.CanManageClaims, ExtraAccessReason.ResponsibleMaster),
            ClaimAccessRequirement.ManageMoney => (Permission.CanManageMoney, ExtraAccessReason.None),
            ClaimAccessRequirement.ManageClaims => (Permission.CanManageClaims, ExtraAccessReason.None),

            // Единственное требование, зависящее от данных: у утверждённой заявки поселение может
            // менять сам игрок или ответственный мастер, у неутверждённой — только обладатель права.
            ClaimAccessRequirement.AccommodationChange => (
                Permission.CanSetPlayersAccommodations,
                claimInfo.Status == ClaimStatus.Approved
                    ? ExtraAccessReason.PlayerOrResponsible
                    : ExtraAccessReason.None),

            _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null),
        };
}
