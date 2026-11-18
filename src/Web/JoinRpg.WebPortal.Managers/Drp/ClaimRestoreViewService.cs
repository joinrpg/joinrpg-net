using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.Data.Write.Interfaces.Notifications;
using JoinRpg.Domain;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Interfaces;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Services.Interfaces.Drp;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;
using JoinRpg.WebPortal.Managers.Claims;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Страница «Восстановление проекта» (DRP, #5481, #5529): план по базе уведомлений и загруженным выгрузкам
/// мастера и применение по одной строке. Права и статус проекта на записи проверяют
/// <see cref="IClaimRestoreService"/> и <see cref="IExportRestoreService"/>.
/// </summary>
internal class ClaimRestoreViewService(
    IProjectMetadataRepository projectMetadataRepository,
    ICurrentUserAccessor currentUserAccessor,
    INotificationRepository notificationRepository,
    ICharacterInfoRepository characterInfoRepository,
    IUserRepository userRepository,
    IUserLinkResolver userLinkResolver,
    IClaimRestoreService claimRestoreService,
    IExportRestoreService exportRestoreService,
    IProjectRestoreLookupRepository restoreLookupRepository,
    IOptions<DrpOptions> options) : IClaimRestoreClient
{
    public async Task<ClaimRestorePlanViewModel> GetPlan(ClaimRestorePlanRequest request)
    {
        var drp = options.Value;
        var projectId = request.ProjectId;
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        if (!projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanManageClaims))
        {
            return ClaimRestorePlanBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.NoAccess, drp);
        }
        if (projectInfo.ProjectStatus != ProjectLifecycleStatus.Blocked)
        {
            return ClaimRestorePlanBuilder.Unavailable(projectInfo, ClaimRestoreUnavailableReason.ProjectNotBlocked, drp);
        }

        List<DatedMasterExport> exports = [.. request.Exports.Select(e => new DatedMasterExport(MasterExportPayload.Deserialize(e.Payload), e.ExportedAt))];

        var notifications = await notificationRepository.GetClaimNotificationsForProject(projectId, drp.LostAt);
        var events = ClaimNotificationParser.Parse(notifications);
        var characters = await characterInfoRepository.GetAllCharacterInfos(projectId);
        var changedNotByRestore = await restoreLookupRepository.GetClaimsCommentedNotByRestoreSince(projectId, drp.LostAt);
        var notificationSource = ClaimRestorePlanBuilder.BuildSource(projectInfo, events, characters, drp.BackupTakenAt, changedNotByRestore);

        // Без выгрузок email и занятые id плану не нужны — пустые списки в базу не ходят.
        var usersByEmail = await restoreLookupRepository.FindUsersByEmails(ProjectRestorePlanViewBuilder.PlayerEmails(exports));
        var takenGroups = await restoreLookupRepository.GetGroupsTakenByOtherProjects(
            ProjectRestorePlanViewBuilder.UnknownGroupIds(exports, projectInfo));
        var takenCharacters = await restoreLookupRepository.GetCharactersTakenByOtherProjects(
            ProjectRestorePlanViewBuilder.UnknownCharacterIds(exports, projectInfo, notificationSource.Characters));

        var source = new ProjectRestoreSource(
            projectInfo,
            notificationSource,
            exports,
            ProjectRestorePlanViewBuilder.ExistingGroups(projectInfo),
            usersByEmail,
            drp.LostAt)
        {
            // Планировщик сверяет с номерами из выгрузки — они числовые.
            GroupIdsTakenByOtherProjects = [.. takenGroups.Select(g => g.CharacterGroupId)],
            CharacterIdsTakenByOtherProjects = [.. takenCharacters.Select(c => c.CharacterId)],
            NewCharacterIsSlot = request.SlotChoices
                .Where(c => c.CharacterId.ProjectId == projectId)
                .DistinctBy(c => c.CharacterId)
                .ToDictionary(c => c.CharacterId, c => c.IsSlot),
        };
        var plan = ProjectRestorePlanner.Plan(source);
        var users = await userRepository.GetUserInfoHeaders(ClaimRestorePlanBuilder.UsersToShow(plan.Claims));

        return ProjectRestorePlanViewBuilder.Build(
            projectInfo, source, plan, users, drp,
            canEditRoles: projectInfo.HasMasterAccess(currentUserAccessor, Permission.CanEditRoles));
    }

    public async Task<MasterExportUploadResult> ParseExport(ProjectIdentification projectId, string fileName, Stream content)
    {
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);
        _ = projectInfo.RequestMasterAccess(currentUserAccessor, Permission.CanManageClaims);
        try
        {
            var export = MasterExportParser.Parse(content, fileName);
            return new MasterExportUploadResult(ProjectRestorePlanViewBuilder.ToUploadView(fileName, export), null);
        }
        catch (MasterExportFormatException ex)
        {
            return new MasterExportUploadResult(null, ProjectRestorePlanViewBuilder.ToErrorView(ex.Error));
        }
    }

    public async Task ApplyGroup(GroupRestoreApplyRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            // TODO[Localize]
            throw new JoinValidationException("Не указано имя группы.");
        }
        await exportRestoreService.RestoreLostGroup(request.GroupId, request.Name.Trim());
    }

    public async Task ApplyCharacter(CharacterRestoreApplyRequest request)
    {
        var target = new RestoredCharacterTarget(request.Groups, ToFields(request.Fields));
        if (!request.Create)
        {
            await exportRestoreService.ApplyRestoredCharacter(request.CharacterId, target);
        }
        else if (string.IsNullOrWhiteSpace(request.Name))
        {
            // TODO[Localize]
            throw new JoinValidationException("Не указано имя персонажа.");
        }
        else
        {
            await exportRestoreService.RestoreLostCharacter(
                request.CharacterId, request.Name.Trim(), request.CharacterType, request.IsHot, RequireVisibility(request.NewCharacterVisibility), target);
        }

        // Видимый всем персонаж открывает восстановленные группы, в которые попал (#5677). Отдельной
        // операцией: при сбое повтор строки найдёт персонажа готовым и доведёт группы.
        if (request.Groups is not null)
        {
            _ = await exportRestoreService.PublishRestoredGroupsOf(request.CharacterId, options.Value.BackupTakenAt);
        }
    }

    /// <summary>
    /// Видимость нового персонажа выбирает мастер (#5677): умолчания нет, чтобы сайт не решал за него,
    /// что увидят игроки.
    /// </summary>
    private static CharacterVisibility RequireVisibility(CharacterVisibility? visibility)
        // TODO[Localize]
        => visibility ?? throw new JoinValidationException("Не выбрана видимость новых персонажей.");

    public async Task ApplyClaimFields(ClaimFieldsRestoreApplyRequest request)
        => await exportRestoreService.ApplyRestoredClaimFields(request.ClaimId, ToFields(request.Fields) ?? new Dictionary<ProjectFieldIdentification, string?>());

    private static Dictionary<ProjectFieldIdentification, string?>? ToFields(IReadOnlyList<RestoreFieldValueRequest> fields)
        => fields.Count == 0 ? null : fields.ToDictionary(f => f.FieldId, f => f.Value);

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
                    new RestoredClaimTarget(Character: null, status, request.StatusChangedAt, ResponsibleMaster: null, request.ManualFee),
                    // Для существующего персонажа видимость не нужна и не используется.
                    request.Character is null ? RequireVisibility(request.NewCharacterVisibility) : CharacterVisibility.Private,
                    request.NewCharacterIsSlot));
                // Для шаблона — сам шаблон: следующие заявки с этим именем подаются на него же.
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
