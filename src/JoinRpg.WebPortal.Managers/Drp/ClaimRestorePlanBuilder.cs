using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Web.Claims;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Перекладка между восстановленной базой, планировщиком восстановления и моделью страницы
/// (<see cref="ClaimRestoreViewService"/>). Без обращений к базе — чтобы проверять тестами.
/// </summary>
internal static class ClaimRestorePlanBuilder
{
    /// <summary>Вход планировщика по событиям из уведомлений и всем персонажам проекта (включая удалённых).</summary>
    public static ClaimRestoreSource BuildSource(
        ProjectInfo projectInfo,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        IReadOnlyCollection<CharacterInfo> characters,
        DateTimeOffset backupTakenAt)
    {
        var existingCharacters = characters
            .Select(c => new ExistingCharacterInfo(
                c.Id,
                c.CharacterName,
                c.IsActive,
                IsSlot: c.CharacterType == CharacterType.Slot,
                DefaultResponsibleMaster: c.ResponsibleMasterId))
            .ToList();

        var claims = characters
            .SelectMany(c => c.Claims.Select(claim => new ExistingClaimInfo(
                claim.ClaimId,
                c.Id,
                claim.Player.UserId,
                claim.Status,
                claim.ResponsibleMasterId,
                claim.Finance.FixedFee)))
            .ToList();

        var masters = projectInfo.Masters
            .Select(m => new ProjectMasterName(m.UserId, m.Name.DisplayName))
            .ToList();

        return new ClaimRestoreSource(
            events,
            claims,
            existingCharacters,
            masters,
            projectInfo.GetDefaultResponsibleMaster().UserId,
            backupTakenAt);
    }

    /// <summary>Пользователи, имена которых нужны для показа плана.</summary>
    public static IReadOnlyCollection<UserIdentification> UsersToShow(IEnumerable<ClaimRestorePlanRow> plan)
        => [.. plan
            .SelectMany(row => row.PlayerCandidates.Prepend(row.Player))
            .OfType<UserIdentification>()
            .Distinct()];

    public static ClaimRestorePlanViewModel Build(
        ProjectInfo projectInfo,
        ClaimRestoreSource source,
        IReadOnlyList<ClaimRestorePlanRow> plan,
        IReadOnlyCollection<UserInfoHeader> users,
        DrpOptions options)
    {
        var characters = source.Characters.ToDictionary(c => c.CharacterId);
        var claims = source.Claims.ToDictionary(c => c.ClaimId);
        var usersById = users.ToDictionary(u => u.UserId);
        var masters = MastersFor(projectInfo);
        // Ответственного выбирают только из списка мастеров: иначе выпадающий список показал бы одного
        // мастера, а отправил другого. Не из списка (например, снятый) — null: для «создать» строка
        // потребует выбора, для «изменить» ответственный не меняется.
        var masterIds = masters.Select(m => m.UserId).ToHashSet();

        ClaimRestoreCharacterOption? ToCharacter(CharacterIdentification? id)
            => id is null ? null : new ClaimRestoreCharacterOption(id, characters.TryGetValue(id, out var c) ? c.Name : id.CharacterId.ToString());

        UserLinkViewModel ToUser(UserIdentification id)
            => usersById.TryGetValue(id, out var user)
                ? new UserLinkViewModel(user)
                : new UserLinkViewModel(id, id.Value.ToString(), ViewMode.Show);

        var rows = plan.Select(row =>
        {
            var existing = claims.GetValueOrDefault(row.ClaimId);
            return new ClaimRestoreRowViewModel(
                row.ClaimId,
                ToActionView(row.Action),
                row.CharacterName,
                CurrentCharacter: ToCharacter(existing?.CharacterId),
                Character: ToCharacter(row.Character),
                CharacterCandidates: [.. row.CharacterCandidates.Select(c => ToCharacter(c)!)],
                Player: row.Player is null ? null : ToUser(row.Player),
                PlayerCandidates: [.. row.PlayerCandidates.Select(ToUser)],
                CurrentStatus: ToStatusView(existing?.Status),
                Status: ToStatusView(row.Status),
                row.StatusChangedAt,
                CurrentResponsibleMaster: existing?.ResponsibleMaster,
                ResponsibleMaster: row.ResponsibleMaster is { } responsible && masterIds.Contains(responsible) ? responsible : null,
                CurrentFee: existing?.ManualFee,
                row.ManualFee,
                row.FirstActivity,
                row.LastActivity,
                Warnings: [.. row.Warnings.Select(ToWarningView)]);
        }).ToList();

        return new ClaimRestorePlanViewModel(
            projectInfo.ProjectId,
            UnavailableReason: null,
            options.BackupTakenAt,
            options.LostAt,
            rows,
            masters);
    }

    public static ClaimRestorePlanViewModel Unavailable(ProjectInfo projectInfo, ClaimRestoreUnavailableReason reason, DrpOptions options)
        => new(projectInfo.ProjectId, reason, options.BackupTakenAt, options.LostAt, [], []);

    private static List<ClaimRestoreMasterOption> MastersFor(ProjectInfo projectInfo)
        => [.. projectInfo.Masters.Select(m => new ClaimRestoreMasterOption(m.UserId, m.Name.DisplayName))];

    // Перечисления страницы повторяют доменные по составу и порядку — так же переводятся статусы во всём портале.
    private static ClaimStatusView? ToStatusView(ClaimStatus? status) => (ClaimStatusView?)status;

    internal static ClaimStatus? FromStatusView(ClaimStatusView? status) => (ClaimStatus?)status;

    private static ClaimRestoreActionView ToActionView(ClaimRestoreAction action) => action switch
    {
        ClaimRestoreAction.Create => ClaimRestoreActionView.Create,
        ClaimRestoreAction.Change => ClaimRestoreActionView.Change,
        ClaimRestoreAction.Nothing => ClaimRestoreActionView.Nothing,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    private static ClaimRestoreWarningView ToWarningView(ClaimRestoreWarning warning) => warning switch
    {
        ClaimRestoreWarning.PlayerNotFound => ClaimRestoreWarningView.PlayerNotFound,
        ClaimRestoreWarning.PlayerAmbiguous => ClaimRestoreWarningView.PlayerAmbiguous,
        ClaimRestoreWarning.ResponsibleNotFound => ClaimRestoreWarningView.ResponsibleNotFound,
        ClaimRestoreWarning.CharacterAmbiguous => ClaimRestoreWarningView.CharacterAmbiguous,
        ClaimRestoreWarning.CharacterNotFound => ClaimRestoreWarningView.CharacterNotFound,
        ClaimRestoreWarning.MovedCharacterNotFound => ClaimRestoreWarningView.MovedCharacterNotFound,
        ClaimRestoreWarning.CharacterRenamed => ClaimRestoreWarningView.CharacterRenamed,
        ClaimRestoreWarning.ApprovedOnSlot => ClaimRestoreWarningView.ApprovedOnSlot,
        ClaimRestoreWarning.StatusUnknown => ClaimRestoreWarningView.StatusUnknown,
        ClaimRestoreWarning.LostClaimNotSubmittedAfterBackup => ClaimRestoreWarningView.LostClaimNotSubmittedAfterBackup,
        _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null),
    };
}
