using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Страница «Восстановление заявок из уведомлений» (DRP, #5481): план по базе уведомлений и применение
/// по одной строке. Права и статус проекта на записи проверяет <see cref="IClaimRestoreService"/>.
/// </summary>
internal class ClaimRestoreViewService(
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    INotificationRepository notificationRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    IUserLinkResolver userLinkResolver,
    IClaimRestoreService claimRestoreService,
    IOptions<DrpOptions> options) : IClaimRestoreClient
{
    public async Task<ClaimRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
    {
        var drp = options.Value;
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageClaims))
        {
            return ClaimRestorePlanBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.NoAccess, drp);
        }
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            return ClaimRestorePlanBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.ProjectNotBlocked, drp);
        }

        var notifications = await notificationRepository.GetClaimNotificationsForProject(projectId, drp.LostAt);
        var events = ClaimNotificationParser.Parse(notifications);
        var characters = await characterInfoRepository.GetAllCharacterInfos(projectId);

        var source = ClaimRestorePlanBuilder.BuildSource(projectInfo, events, characters, drp.BackupTakenAt);
        var plan = ClaimRestorePlanner.Plan(source);
        var users = await userRepository.GetUserInfoHeaders(ClaimRestorePlanBuilder.UsersToShow(plan));

        return ClaimRestorePlanBuilder.Build(projectInfo, source, plan, users, drp);
    }

    public async Task<ClaimRestoreApplyResult> Apply(ClaimRestoreApplyRequest request)
    {
        var status = ClaimRestorePlanBuilder.FromStatusView(request.Status);
        switch (request.Action)
        {
            case ClaimRestoreActionView.Create:
                var player = await ResolvePlayer(request);
                // TODO[Localize]
                var responsible = request.ResponsibleMaster ?? throw new JoinValidationException("Не выбран ответственный мастер.");
                var character = await claimRestoreService.RestoreLostClaim(new LostClaimToRestore(
                    request.ClaimId,
                    request.Character,
                    request.NewCharacterName,
                    player,
                    responsible,
                    request.CreatedAt,
                    new RestoredClaimTarget(Character: null, status, request.StatusChangedAt, ResponsibleMaster: null, request.ManualFee)));
                return new ClaimRestoreApplyResult(character);

            case ClaimRestoreActionView.Change:
                await claimRestoreService.ApplyRestoredState(
                    request.ClaimId,
                    new RestoredClaimTarget(request.Character, status, request.StatusChangedAt, request.ResponsibleMaster, request.ManualFee));
                return new ClaimRestoreApplyResult(request.Character);

            default:
                return new ClaimRestoreApplyResult(null);
        }
    }

    private async Task<UserIdentification> ResolvePlayer(ClaimRestoreApplyRequest request)
    {
        if (request.Player is { } player)
        {
            return player;
        }
        if (!string.IsNullOrWhiteSpace(request.PlayerLink))
        {
            return await userLinkResolver.ResolveAsync(request.PlayerLink);
        }
        // TODO[Localize]
        throw new JoinValidationException("Не указан игрок.");
    }
}
