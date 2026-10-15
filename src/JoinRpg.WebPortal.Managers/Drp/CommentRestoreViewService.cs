using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Domain;
using JoinRpg.Interfaces;
using JoinRpg.Services.Drp.ClaimComments;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Секция «Комментарии к заявкам» страницы «Восстановление проекта» (DRP, #5492): план по базе
/// уведомлений и применение по одной заявке. Права и статус проекта на записи ещё раз проверяет
/// <see cref="IClaimRestoreService"/>.
/// </summary>
internal class CommentRestoreViewService(
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IClaimRestoreService claimRestoreService,
    IProjectRestoreLookupRepository restoreLookupRepository,
    IOptions<DrpOptions> options) : ICommentRestoreClient
{
    public async Task<CommentRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageClaims))
        {
            return CommentRestorePlanViewBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.NoAccess);
        }
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            return CommentRestorePlanViewBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.ProjectNotBlocked);
        }

        var drp = options.Value;
        var events = ClaimNotificationParser.Parse(await notificationRepository.GetClaimNotificationsForProject(projectId, drp.LostAt));
        var lookup = await restoreLookupRepository.GetClaimCommentsForRestore(
            projectId, CommentRestorePlanViewBuilder.NotifiedCommentIds(events), TextsSince(drp));
        var plan = Plan(projectInfo, events, lookup);
        var users = await userRepository.GetUserInfoHeaders(CommentRestorePlanViewBuilder.UsersToShow(plan, lookup));
        return CommentRestorePlanViewBuilder.Build(projectInfo, plan, lookup, users);
    }

    public async Task<CommentRestoreApplyResult> Apply(ClaimIdentification claimId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            // TODO[Localize]
            throw new JoinValidationException("Восстановление из уведомлений доступно только в заблокированном проекте.");
        }

        // План — заново и на сервере (клиент присылает только номер заявки), по событиям и комментариям
        // только этой заявки: план одной заявки от других не зависит.
        var drp = options.Value;
        var events = ClaimNotificationParser.Parse(await notificationRepository.GetClaimNotificationsForClaim(claimId, drp.LostAt));
        var lookup = await restoreLookupRepository.GetClaimCommentsForRestore(
            claimId, CommentRestorePlanViewBuilder.NotifiedCommentIds(events), TextsSince(drp));
        var row = Plan(projectInfo, events, lookup).SingleOrDefault(r => r.ClaimId == claimId);
        if (row is null || (row.ClaimExists && row.ToRestore.Count == 0))
        {
            return new CommentRestoreApplyResult(0, 0, []);
        }
        if (!row.ClaimExists)
        {
            // TODO[Localize]
            throw new JoinValidationException("Заявки нет в базе — сначала восстановите заявку.");
        }

        var result = await claimRestoreService.RestoreClaimComments(claimId, CommentRestorePlanViewBuilder.ToRestore(row));
        return new CommentRestoreApplyResult(result.Inserted, result.AlreadyExisted, result.Conflicts);
    }

    private IReadOnlyList<CommentRestorePlanRow> Plan(
        ProjectInfo projectInfo, IReadOnlyCollection<ClaimNotificationEvent> events, ClaimCommentsRestoreLookup lookup)
        => CommentRestorePlanner.Plan(CommentRestorePlanViewBuilder.BuildSource(projectInfo, events, lookup, options.Value.BackupTakenAt));

    /// <summary>
    /// Тексты из базы нужны только для сопоставления комментариев без id с событиями после бэкапа,
    /// а те совпадают по времени с точностью до минуты (<see cref="CommentRestorePlanner.SameTimeTolerance"/>).
    /// </summary>
    private static DateTimeOffset TextsSince(DrpOptions drp) => drp.BackupTakenAt - CommentRestorePlanner.SameTimeTolerance;
}
