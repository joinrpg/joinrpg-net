using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Notifications;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.ManualFinance;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Секция «Ручные оплаты» страницы «Восстановление проекта» (DRP, #5631): план по базе уведомлений и
/// применение по одной заявке с типами оплаты, выбранными мастером. Права и статус проекта на записи ещё раз
/// проверяют <see cref="IClaimRestoreService"/> и <see cref="IProjectFinanceSettingsService"/>.
/// </summary>
internal class FinanceRestoreViewService(
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    INotificationRepository notificationRepository,
    IUserRepository userRepository,
    IClaimsRepository claimsRepository,
    IClaimRestoreService claimRestoreService,
    IProjectFinanceSettingsService financeSettingsService,
    IProjectRestoreLookupRepository restoreLookupRepository,
    IOptions<DrpOptions> options) : IFinanceRestoreClient
{
    public async Task<FinanceRestorePlanViewModel> GetPlan(ProjectIdentification projectId)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageMoney))
        {
            return FinanceRestorePlanViewBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.NoAccess);
        }
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            return FinanceRestorePlanViewBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.ProjectNotBlocked);
        }

        var plan = await Plan(projectId, await notificationRepository.GetClaimNotificationsForProject(projectId, options.Value.LostAt));
        var existingClaims = FinanceRestorePlanViewBuilder.ExistingClaims(plan);
        var claims = existingClaims.Count == 0 ? [] : await claimsRepository.GetClaimHeadersWithPlayer(existingClaims);
        var users = await userRepository.GetUserInfoHeaders(FinanceRestorePlanViewBuilder.UsersToShow(plan));
        return FinanceRestorePlanViewBuilder.Build(projectInfo, currentUserAccessor.UserIdentification, plan, claims, users);
    }

    public async Task<FinanceRestoreApplyResult> Apply(FinanceRestoreApplyRequest request)
    {
        var claimId = request.ClaimId;
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(claimId.ProjectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageMoney);
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            // TODO[Localize]
            throw new JoinValidationException("Восстановление из уведомлений доступно только в заблокированном проекте.");
        }

        // План — заново и на сервере, по уведомлениям только этой заявки: план заявки от уведомлений других
        // не зависит. Выборка из базы — по проекту: номер из уведомления может быть занят другой заявкой.
        var plan = await Plan(claimId.ProjectId, await notificationRepository.GetClaimNotificationsForClaim(claimId, options.Value.LostAt));
        var row = plan.SingleOrDefault(r => r.ClaimId == claimId);
        if (row is { ClaimExists: false })
        {
            // TODO[Localize]
            throw new JoinValidationException("Заявки нет в базе — сначала восстановите заявку.");
        }
        EnsureValidPaymentTypes(projectInfo, row, request.PaymentTypes);
        if (row is null)
        {
            return new FinanceRestoreApplyResult(0, 0, 0, 0, []);
        }

        var input = FinanceRestoreInputBuilder.BuildClaim(
            row,
            op => request.PaymentTypes.TryGetValue(op.Key, out var id) ? new PaymentTypeIdentification(claimId.ProjectId, id) : null);
        var skipped = FinanceRestorePlanViewBuilder.Skipped(row, input, projectInfo.TimeZone);
        if (input.IsEmpty)
        {
            return new FinanceRestoreApplyResult(0, 0, 0, 0, skipped);
        }

        var result = await claimRestoreService.RestoreFinanceOperations(claimId, input.Operations, input.Moderations);
        return new FinanceRestoreApplyResult(result.Inserted, result.AlreadyExisted, result.Conflicts, result.ParentsNotRestored, skipped);
    }

    public async Task CreateOwnCash(ProjectIdentification projectId)
        => _ = await financeSettingsService.CreateRestoredCashPaymentType(projectId, currentUserAccessor.UserIdentification);

    /// <summary>Каждый присланный тип — неонлайн-тип этого проекта, у операции, которая есть в плане заявки.</summary>
    private static void EnsureValidPaymentTypes(ProjectInfo projectInfo, FinanceRestorePlanRow? row, IReadOnlyDictionary<string, int> paymentTypes)
    {
        var keys = row?.Operations.Select(o => o.Key).ToHashSet() ?? [];
        foreach (var (key, id) in paymentTypes)
        {
            // TODO[Localize]
            var paymentType = projectInfo.ProjectFinanceSettings.GetPaymentByIdOrDefault(new PaymentTypeIdentification(projectInfo.ProjectId, id))
                ?? throw new JoinValidationException($"Тип оплаты {id} не найден в проекте.");
            if (!FinanceRestorePlanViewBuilder.IsManual(paymentType.TypeKind))
            {
                // TODO[Localize]
                throw new JoinValidationException($"Тип оплаты «{paymentType.Name}» — онлайн: ручные оплаты на него не восстанавливаются.");
            }
            if (!keys.Contains(key))
            {
                // TODO[Localize]
                throw new JoinValidationException("Операции нет в плане восстановления заявки — обновите план.");
            }
        }
    }

    private async Task<IReadOnlyList<FinanceRestorePlanRow>> Plan(
        ProjectIdentification projectId, IReadOnlyCollection<NotificationMessageForRecipient> notifications)
    {
        var drp = options.Value;
        var events = ClaimNotificationParser.Parse(notifications);
        var lookup = await restoreLookupRepository.GetClaimFinanceForRestore(
            projectId, FinanceRestorePlanner.NotifiedCommentIds(events, drp.BackupTakenAt));
        return FinanceRestorePlanner.Plan(FinanceRestoreInputBuilder.BuildSource(lookup, events, drp.BackupTakenAt));
    }
}
