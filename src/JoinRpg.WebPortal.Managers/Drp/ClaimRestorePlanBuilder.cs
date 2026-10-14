using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.Common.WebComponents;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp;
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
    /// <summary>
    /// Вход планировщика по событиям из уведомлений и всем персонажам проекта (включая удалённых).
    /// Слои полей, обычные группы и даты изменения нужны только плану по выгрузкам (#5529), но берутся
    /// из тех же агрегатов — без лишних запросов.
    /// </summary>
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
                DefaultResponsibleMaster: c.ResponsibleMasterId)
            {
                Groups = [.. c.DirectGroupIds.Where(g => projectInfo.GroupTree.GetGroupByIdOrDefault(g)?.GroupType == CharacterGroupType.Regular)],
                FieldLayer = CharacterLayer(projectInfo, c.CharacterFields),
                UpdatedAt = AsUtc(c.UpdatedAt),
            })
            .ToList();

        var claims = characters
            .SelectMany(c => c.Claims.Select(claim => new ExistingClaimInfo(
                claim.ClaimId,
                c.Id,
                claim.Player.UserId,
                claim.Status,
                claim.ResponsibleMasterId,
                claim.Finance.FixedFee)
            {
                // Слой заявки как есть (Claim.JsonData): поля заявки и предложения игрока.
                FieldLayer = claim.Fields,
                LastUpdated = AsUtc(claim.LastUpdateDateTime),
            }))
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

    /// <summary>Слой персонажа без полей заявки — как его ждёт <see cref="ExistingCharacterInfo.FieldLayer"/>.</summary>
    private static FieldLayerContainer CharacterLayer(ProjectInfo projectInfo, FieldLayerContainer layer)
        => new(projectInfo, layer.LayerData
            .Where(f => f.Value.Field.BoundTo == FieldBoundTo.Character)
            .ToDictionary(f => f.Key, f => f.Value));

    /// <summary>Даты изменения в базе хранятся в UTC.</summary>
    private static DateTimeOffset AsUtc(DateTime value) => new(DateTime.SpecifyKind(value, DateTimeKind.Utc));

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
        DrpOptions options,
        IReadOnlyDictionary<CharacterIdentification, string>? plannedCharacterNames = null)
    {
        var characters = source.Characters.ToDictionary(c => c.CharacterId, c => c.Name);
        foreach (var (id, name) in plannedCharacterNames ?? new Dictionary<CharacterIdentification, string>())
        {
            _ = characters.TryAdd(id, name);
        }
        var claims = source.Claims.ToDictionary(c => c.ClaimId);
        var usersById = users.ToDictionary(u => u.UserId);
        var masters = MastersFor(projectInfo);
        // Ответственного выбирают только из списка мастеров: иначе выпадающий список показал бы одного
        // мастера, а отправил другого. Не из списка (например, снятый) — null: для «создать» строка
        // потребует выбора, для «изменить» ответственный не меняется.
        var masterIds = masters.Select(m => m.UserId).ToHashSet();

        ClaimRestoreCharacterOption? ToCharacter(CharacterIdentification? id)
            => id is null ? null : new ClaimRestoreCharacterOption(id, characters.TryGetValue(id, out var name) ? name : id.CharacterId.ToString());

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
                Warnings: [.. row.Warnings.Select(ToWarningView)])
            {
                ApplyByDefault = row.ApplyByDefault,
                Attributes = ToAttributesView(row.Attributes),
                AttributesNotByDefault = [.. row.AttributesNotByDefault.Select(ToAttributeKindView)],
            };
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

    internal static ClaimRestoreWarningView ToWarningView(ClaimRestoreWarning warning) => warning switch
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
        ClaimRestoreWarning.ChangedAfterRestore => ClaimRestoreWarningView.ChangedAfterRestore,
        ClaimRestoreWarning.TotalFeeMismatch => ClaimRestoreWarningView.TotalFeeMismatch,
        _ => throw new ArgumentOutOfRangeException(nameof(warning), warning, null),
    };

    internal static ClaimRestoreAttributeKindView ToAttributeKindView(ClaimRestoreAttributeKind kind) => kind switch
    {
        ClaimRestoreAttributeKind.Status => ClaimRestoreAttributeKindView.Status,
        ClaimRestoreAttributeKind.ResponsibleMaster => ClaimRestoreAttributeKindView.ResponsibleMaster,
        ClaimRestoreAttributeKind.Character => ClaimRestoreAttributeKindView.Character,
        ClaimRestoreAttributeKind.ManualFee => ClaimRestoreAttributeKindView.ManualFee,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    internal static RestoreSourceKindView ToSourceView(RestoreSourceKind source) => source switch
    {
        RestoreSourceKind.Database => RestoreSourceKindView.Database,
        RestoreSourceKind.Export => RestoreSourceKindView.Export,
        RestoreSourceKind.Notification => RestoreSourceKindView.Notification,
        _ => throw new ArgumentOutOfRangeException(nameof(source), source, null),
    };

    private static ClaimRestoreAttributesView? ToAttributesView(ClaimRestoreAttributes? attributes)
        => attributes is null
            ? null
            : new ClaimRestoreAttributesView(
                ToAttributeView(attributes.Status, s => (ClaimStatusView)s),
                ToAttributeView(attributes.ResponsibleMaster, u => u),
                ToAttributeView(attributes.CharacterName, n => n));

    private static RestoreAttributeView<TView>? ToAttributeView<T, TView>(RestoreAttribute<T>? attribute, Func<T, TView> map)
    {
        if (attribute is null)
        {
            return null;
        }
        RestoreSourcedValueView<TView> ToValue(RestoreSourcedValue<T> value) => new(map(value.Value), ToSourceView(value.Source), value.At);
        return new RestoreAttributeView<TView>(ToValue(attribute.Chosen), [.. attribute.Alternatives.Select(ToValue)]);
    }
}
