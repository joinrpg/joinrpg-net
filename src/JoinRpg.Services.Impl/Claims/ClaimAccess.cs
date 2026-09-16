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
    public static void Request(
        ProjectInfo projectInfo,
        CharacterClaimInfo claimInfo,
        ICurrentUserAccessor currentUser,
        ClaimAccessRequirement requirement)
    {
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

    private static (Permission Permission, ExtraAccessReason Reason) Resolve(
        ClaimAccessRequirement requirement,
        CharacterClaimInfo claimInfo)
        => requirement switch
        {
            ClaimAccessRequirement.MasterOrPlayer => (Permission.None, ExtraAccessReason.Player),
            ClaimAccessRequirement.AnyMaster => (Permission.None, ExtraAccessReason.None),
            ClaimAccessRequirement.ApprovalDecline => (Permission.CanManageClaims, ExtraAccessReason.ResponsibleMaster),
            ClaimAccessRequirement.ManageMoney => (Permission.CanManageMoney, ExtraAccessReason.None),

            // Единственное требование, зависящее от данных: у утверждённой заявки поселение может
            // менять сам игрок или ответственный мастер, у неутверждённой — только обладатель права.
            //
            // Здесь намеренно строгое сравнение с Approved, а не Claim.IsApproved (который включает
            // и CheckedIn): игрок не должен менять себе поселение после регистрации на игре. До
            // миграции это выражалось тем же сравнением плюс комментарием «Player cannot change
            // accommodation type if already checked in» — теперь намерение записано там, где правило.
            //
            // Из UI ветка «заявка не утверждена» недостижима: панель проживания на странице заявки
            // рендерится редактируемой только при ClaimStatus == Approved, при остальных статусах
            // показывается read-only. Правило остаётся серверной защитой прямого вызова API —
            // эндпоинты собственной проверки статуса не имеют.
            ClaimAccessRequirement.AccommodationChange => (
                Permission.CanSetPlayersAccommodations,
                claimInfo.Status == ClaimStatus.Approved
                    ? ExtraAccessReason.PlayerOrResponsible
                    : ExtraAccessReason.None),

            _ => throw new ArgumentOutOfRangeException(nameof(requirement), requirement, null),
        };
}
