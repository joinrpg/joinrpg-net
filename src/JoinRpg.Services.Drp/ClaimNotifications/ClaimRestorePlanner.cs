using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.Services.Drp.MasterExports;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>Заявка из выгрузки заявок — снимок на дату выгрузки.</summary>
internal record ExportedClaimSnapshot(MasterExportRow Row, DateTimeOffset ExportedAt);

/// <summary>Что о заявках и персонажах известно из выгрузок мастера (#5529).</summary>
/// <param name="Claims">Все строки выгрузок заявок для каждой заявки — от старой выгрузки к новой.</param>
/// <param name="UsersByEmail">Пользователи по email — для игрока заявки из выгрузки.</param>
/// <param name="CharacterPlayers">Игрок персонажа по выгрузке персонажей — чтобы различить тёзок.</param>
/// <param name="ExportedCharacterNames">Имя персонажа по самой свежей выгрузке персонажей.</param>
/// <param name="LostAt">Момент аварии: заявки, изменённые позже, могли править руками после восстановления.</param>
internal record ClaimExportContext(
    IReadOnlyDictionary<ClaimIdentification, IReadOnlyList<ExportedClaimSnapshot>> Claims,
    IReadOnlyDictionary<string, UserIdentification> UsersByEmail,
    IReadOnlyDictionary<CharacterIdentification, UserIdentification> CharacterPlayers,
    IReadOnlyDictionary<CharacterIdentification, string> ExportedCharacterNames,
    DateTimeOffset? LostAt)
{
    /// <summary>Выгрузок нет — только уведомления.</summary>
    public static ClaimExportContext None { get; } = new(
        new Dictionary<ClaimIdentification, IReadOnlyList<ExportedClaimSnapshot>>(),
        new Dictionary<string, UserIdentification>(),
        new Dictionary<CharacterIdentification, UserIdentification>(),
        new Dictionary<CharacterIdentification, string>(),
        null);
}

/// <summary>
/// Сверяет то, что известно о заявках по уведомлениям (и выгрузкам мастера), с восстановленной базой
/// и предлагает по одному действию на заявку (DRP, #5481, #5529).
/// </summary>
/// <remarks>
/// У каждой заявки своя «база отсчёта» — самый свежий снимок из восстановленной базы (на момент бэкапа)
/// и выгрузки заявок (на её дату). Поверх неё проигрываются только события после неё. Ручной взнос
/// в выгрузке не виден, поэтому для него база отсчёта — всегда восстановленная база.
/// </remarks>
public static class ClaimRestorePlanner
{
    /// <summary>Входные данные и индексы по ним — строятся один раз на план.</summary>
    private sealed class Planning(ClaimRestoreSource source, ClaimExportContext exports)
    {
        public ClaimRestoreSource Source { get; } = source;
        public ClaimExportContext Exports { get; } = exports;
        public DateTimeOffset BackupTakenAt => Source.BackupTakenAt;

        public Dictionary<CharacterIdentification, ExistingCharacterInfo> Characters { get; } =
            source.Characters.ToDictionary(c => c.CharacterId);

        private readonly ILookup<string, ExistingCharacterInfo> activeByName =
            source.Characters.Where(c => c.IsActive).ToLookup(c => c.Name.Trim());

        private readonly ILookup<string, UserIdentification> mastersByName =
            source.Masters.ToLookup(m => m.DisplayName.Trim(), m => m.UserId);

        /// <summary>Активные персонажи проекта с таким именем (без учёта пробелов по краям).</summary>
        public IReadOnlyList<ExistingCharacterInfo> FindCharactersByName(string name) => [.. activeByName[name.Trim()]];

        public UserIdentification DefaultResponsible(CharacterIdentification? character)
            => character is not null && Characters.TryGetValue(character, out var found)
                ? found.DefaultResponsibleMaster
                : Source.DefaultResponsibleMaster;

        /// <summary>
        /// Мастер по отображаемому имени. null — имени нет (без пометки) или оно не сопоставилось однозначно
        /// (с пометкой <see cref="ClaimRestoreWarning.ResponsibleNotFound"/>).
        /// </summary>
        public UserIdentification? FindMaster(string? name, List<ClaimRestoreWarning> warnings)
        {
            if (name is null)
            {
                return null;
            }
            var matches = mastersByName[name.Trim()].ToList();
            if (matches.Count == 1)
            {
                return matches[0];
            }
            if (!warnings.Contains(ClaimRestoreWarning.ResponsibleNotFound))
            {
                warnings.Add(ClaimRestoreWarning.ResponsibleNotFound);
            }
            return null;
        }

        /// <summary>Имя персонажа — в базе или в самой свежей выгрузке персонажей.</summary>
        public bool IsNameOf(string name, ExistingCharacterInfo? character)
            => SameName(name, character?.Name)
            || (character is not null && SameName(name, Exports.ExportedCharacterNames.GetValueOrDefault(character.CharacterId)));
    }

    /// <summary>План только по уведомлениям (#5481).</summary>
    /// <remarks>
    /// Момент аварии здесь неизвестен, поэтому «правил руками после восстановления»
    /// (<see cref="ClaimRestoreWarning.ChangedAfterRestore"/>) не распознаётся: всё, что расходится
    /// с уведомлениями, применяется по умолчанию. <see cref="ProjectRestorePlanner"/> без выгрузок даёт те же
    /// строки, но распознаёт правки (по <see cref="ExistingClaimInfo.LastUpdated"/> позже аварии) — новая
    /// страница восстановления (#5529) зовёт его.
    /// </remarks>
    /// <returns>
    /// Строки в порядке применения: сначала те, после которых заявка не принята, — они освобождают
    /// персонажей для принятых. Заявки без событий после бэкапа в план не попадают.
    /// </returns>
    public static IReadOnlyList<ClaimRestorePlanRow> Plan(ClaimRestoreSource source) => Plan(source, ClaimExportContext.None);

    /// <summary>План по уведомлениям и выгрузкам заявок (#5529).</summary>
    /// <returns>
    /// Строки в том же порядке. Заявка из базы без событий после бэкапа попадает в план, только если есть
    /// в выгрузке заявок новее бэкапа; заявка, которой нет в базе, но есть в выгрузке, — всегда.
    /// </returns>
    internal static IReadOnlyList<ClaimRestorePlanRow> Plan(ClaimRestoreSource source, ClaimExportContext exports)
    {
        var p = new Planning(source, exports);
        var claims = source.Claims.ToDictionary(c => c.ClaimId);
        var events = source.Events.GroupBy(e => e.ClaimId).ToDictionary(g => g.Key, g => g.ToList());

        var rows = new List<(ClaimRestorePlanRow Row, ClaimStatus? FinalStatus)>();
        foreach (var claimId in events.Keys.Concat(exports.Claims.Keys).Distinct())
        {
            IReadOnlyCollection<ClaimNotificationEvent> claimEvents = events.GetValueOrDefault(claimId) ?? [];
            var snapshots = exports.Claims.GetValueOrDefault(claimId) ?? [];
            var latest = snapshots.Count > 0 ? snapshots[^1] : null;
            if (claims.TryGetValue(claimId, out var existing))
            {
                IReadOnlyCollection<ClaimNotificationEvent> afterBackup = [.. claimEvents.Where(e => e.CreatedAt >= source.BackupTakenAt)];
                var row = latest is not null && latest.ExportedAt > source.BackupTakenAt
                    ? PlanExistingFromExport(p, existing, latest, claimEvents, afterBackup)
                    : PlanExisting(p, existing, afterBackup, latest);
                if (row is not null)
                {
                    row = MarkChangedAfterRestore(p, row, existing, snapshots, claimEvents);
                    rows.Add((row, row.Status ?? existing.Status));
                }
            }
            else
            {
                var row = latest is not null
                    ? PlanFromExport(p, claimId, latest, claimEvents)
                    : PlanLost(p, claimId, claimEvents);
                rows.Add((row, row.Status));
            }
        }

        return [.. rows
            .OrderBy(r => r.FinalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn)
            .ThenBy(r => r.Row.ClaimId.ClaimId)
            .Select(r => r.Row)];
    }

    private static ClaimRestorePlanRow PlanLost(Planning p, ClaimIdentification claimId, IReadOnlyCollection<ClaimNotificationEvent> events)
    {
        var state = RestoredClaimStateBuilder.Build(claimId, events);
        var warnings = ConvertWarnings(state.Warnings);

        var (character, candidates) = FindLostClaimCharacter(p, [state.CharacterName], state.Player, state.Status, warnings);

        var eventResponsible = p.FindMaster(state.ResponsibleMasterName, warnings);
        var responsible = eventResponsible ?? p.DefaultResponsible(character);

        if (state.Status is null)
        {
            warnings.Add(ClaimRestoreWarning.StatusUnknown);
        }
        if (!state.IsNewClaim || state.FirstActivity < p.BackupTakenAt)
        {
            warnings.Add(ClaimRestoreWarning.LostClaimNotSubmittedAfterBackup);
        }

        return new ClaimRestorePlanRow(
            claimId,
            ClaimRestoreAction.Create,
            state.CharacterName,
            character,
            candidates,
            state.Player,
            state.PlayerCandidates,
            state.Status,
            state.StatusChangedAt,
            responsible,
            state.ManualFee,
            state.FirstActivity,
            state.LastActivity,
            warnings)
        {
            Attributes = BuildAttributes(p, null, null, null, null, state, eventResponsible),
        };
    }

    /// <summary>Заявки нет в базе, но она есть в выгрузке заявок: создать по выгрузке и событиям после неё.</summary>
    private static ClaimRestorePlanRow PlanFromExport(
        Planning p, ClaimIdentification claimId, ExportedClaimSnapshot exported, IReadOnlyCollection<ClaimNotificationEvent> events)
    {
        var row = exported.Row;
        var warnings = new List<ClaimRestoreWarning>();

        // Игрок — по email из выгрузки; если такого пользователя нет — по уведомлениям.
        var fullState = events.Count > 0 ? RestoredClaimStateBuilder.Build(claimId, events) : null;
        UserIdentification? player;
        IReadOnlyCollection<UserIdentification> playerCandidates;
        if (FindUserByEmail(p.Exports, row.PlayerEmail) is { } byEmail)
        {
            player = byEmail;
            playerCandidates = [byEmail];
        }
        else
        {
            player = fullState?.Player;
            playerCandidates = fullState?.PlayerCandidates ?? [];
            if (player is null)
            {
                warnings.Add(playerCandidates.Count > 1 ? ClaimRestoreWarning.PlayerAmbiguous : ClaimRestoreWarning.PlayerNotFound);
            }
        }

        IReadOnlyCollection<ClaimNotificationEvent> afterExport = [.. events.Where(e => e.CreatedAt >= exported.ExportedAt)];
        var state = afterExport.Count > 0 ? RestoredClaimStateBuilder.Build(claimId, afterExport, row.Status, player) : null;

        var status = state?.Status ?? row.Status;
        if (status is null)
        {
            warnings.Add(ClaimRestoreWarning.StatusUnknown);
        }

        var characterName = state?.CharacterName ?? row.Name;
        var (character, candidates) = FindLostClaimCharacter(p, [state?.CharacterName, row.Name], player, status, warnings);

        var exportResponsible = p.FindMaster(row.ResponsibleMasterName, warnings);
        var eventResponsible = state is null ? null : p.FindMaster(state.ResponsibleMasterName, warnings);
        var responsible = eventResponsible ?? exportResponsible ?? p.DefaultResponsible(character);

        var manualFee = fullState?.ManualFee;
        CheckTotalFee(row, manualFee, state, warnings);
        // Заявки в базе нет — и оплат по ней нет.
        var feePaidMismatch = CheckFeePaid(row, feePaidInDatabase: 0, warnings);

        if ((row.ClaimCreatedAt ?? exported.ExportedAt) < p.BackupTakenAt)
        {
            warnings.Add(ClaimRestoreWarning.LostClaimNotSubmittedAfterBackup);
        }

        var (first, last) = Activity(exported, events);
        return new ClaimRestorePlanRow(
            claimId,
            ClaimRestoreAction.Create,
            characterName,
            character,
            candidates,
            player,
            playerCandidates,
            status,
            status is null ? null : state?.StatusChangedAt ?? row.ClaimUpdatedAt ?? exported.ExportedAt,
            responsible,
            manualFee,
            first,
            last,
            warnings)
        {
            Attributes = BuildAttributes(p, null, null, exported, exportResponsible, state, eventResponsible),
            FeePaidMismatch = feePaidMismatch,
        };
    }

    private static ClaimRestorePlanRow? PlanExisting(
        Planning p,
        ExistingClaimInfo existing,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        ExportedClaimSnapshot? olderExport)
    {
        if (events.Count == 0)
        {
            return null;
        }

        var state = RestoredClaimStateBuilder.Build(existing.ClaimId, events, existing.Status, existing.Player);
        // Игрок у заявки из базы известен — неуверенность в нём по уведомлениям не важна.
        var warnings = new List<ClaimRestoreWarning>();

        // Перемещение между тёзками по имени не видно — такое мастер поправит сам.
        CharacterIdentification? moveTo = null;
        IReadOnlyCollection<CharacterIdentification> candidates = [];
        var current = p.Characters.GetValueOrDefault(existing.CharacterId);
        if (state.CharacterName is not null && !p.IsNameOf(state.CharacterName, current))
        {
            if (state.MayHaveChangedCharacter)
            {
                (moveTo, candidates) = FindMoveTarget(p, existing, state.CharacterName, warnings);
            }
            else
            {
                warnings.Add(ClaimRestoreWarning.CharacterRenamed);
            }
        }

        ClaimStatus? status = state.Status != existing.Status ? state.Status : null;
        CheckApprovedOnSlot(p, status ?? existing.Status, moveTo, current, warnings);

        var responsible = p.FindMaster(state.ResponsibleMasterName, warnings);
        UserIdentification? responsibleChange = responsible is not null && responsible != existing.ResponsibleMaster ? responsible : null;
        int? feeChange = state.ManualFee is not null && state.ManualFee != existing.ManualFee ? state.ManualFee : null;

        var changed = moveTo is not null || status is not null || responsibleChange is not null || feeChange is not null;

        // Выгрузка старше бэкапа в расчёт не идёт, но её значения показываются как альтернатива.
        var exportResponsible = olderExport is null ? null : p.FindMaster(olderExport.Row.ResponsibleMasterName, []);
        return new ClaimRestorePlanRow(
            existing.ClaimId,
            changed ? ClaimRestoreAction.Change : ClaimRestoreAction.Nothing,
            state.CharacterName,
            moveTo,
            candidates,
            existing.Player,
            [existing.Player],
            status,
            status is null ? null : state.StatusChangedAt,
            responsibleChange,
            feeChange,
            state.FirstActivity,
            state.LastActivity,
            warnings)
        {
            Attributes = BuildAttributes(p, existing, current?.Name, olderExport, exportResponsible, state, responsible),
        };
    }

    /// <summary>
    /// Заявка есть в базе, а выгрузка заявок свежее бэкапа: база отсчёта — выгрузка, поверх неё события
    /// после выгрузки. Ручной взнос — по событиям после бэкапа, его в выгрузке нет.
    /// </summary>
    private static ClaimRestorePlanRow PlanExistingFromExport(
        Planning p,
        ExistingClaimInfo existing,
        ExportedClaimSnapshot exported,
        IReadOnlyCollection<ClaimNotificationEvent> events,
        IReadOnlyCollection<ClaimNotificationEvent> afterBackup)
    {
        var row = exported.Row;
        var warnings = new List<ClaimRestoreWarning>();

        var baseStatus = row.Status ?? existing.Status;
        var state = BuildAfter(existing, events, exported.ExportedAt, baseStatus);

        var current = p.Characters.GetValueOrDefault(existing.CharacterId);
        var characterName = state?.CharacterName ?? row.Name;
        CharacterIdentification? moveTo = null;
        IReadOnlyCollection<CharacterIdentification> candidates = [];
        if (characterName is not null && !p.IsNameOf(characterName, current))
        {
            // Имя из выгрузки (или подтверждённое ею) другое — заявку перемещали до выгрузки.
            if (state?.MayHaveChangedCharacter == true || SameName(characterName, row.Name))
            {
                (moveTo, candidates) = FindMoveTarget(p, existing, characterName, warnings);
            }
            else
            {
                warnings.Add(ClaimRestoreWarning.CharacterRenamed);
            }
        }

        var finalStatus = state?.Status ?? baseStatus;
        ClaimStatus? status = finalStatus != existing.Status ? finalStatus : null;
        CheckApprovedOnSlot(p, finalStatus, moveTo, current, warnings);

        var exportResponsible = p.FindMaster(row.ResponsibleMasterName, warnings);
        var eventResponsible = state is null ? null : p.FindMaster(state.ResponsibleMasterName, warnings);
        var responsible = eventResponsible ?? exportResponsible;
        UserIdentification? responsibleChange = responsible is not null && responsible != existing.ResponsibleMaster ? responsible : null;

        var feeState = afterBackup.Count > 0
            ? RestoredClaimStateBuilder.Build(existing.ClaimId, afterBackup, existing.Status, existing.Player)
            : null;
        int? feeChange = feeState?.ManualFee is { } fee && fee != existing.ManualFee ? fee : null;
        CheckTotalFee(row, feeChange ?? existing.ManualFee, state, warnings);
        var feePaidMismatch = CheckFeePaid(row, existing.FeePaid, warnings);

        var changed = moveTo is not null || status is not null || responsibleChange is not null || feeChange is not null;

        var (first, last) = Activity(exported, afterBackup);
        return new ClaimRestorePlanRow(
            existing.ClaimId,
            changed ? ClaimRestoreAction.Change : ClaimRestoreAction.Nothing,
            characterName,
            moveTo,
            candidates,
            existing.Player,
            [existing.Player],
            status,
            status is null ? null : state?.StatusChangedAt ?? row.ClaimUpdatedAt ?? exported.ExportedAt,
            responsibleChange,
            feeChange,
            first,
            last,
            warnings)
        {
            Attributes = BuildAttributes(p, existing, current?.Name, exported, exportResponsible, state, eventResponsible),
            FeePaidMismatch = feePaidMismatch,
        };
    }

    private static RestoredClaimState? BuildAfter(
        ExistingClaimInfo existing, IEnumerable<ClaimNotificationEvent> events, DateTimeOffset after, ClaimStatus baseStatus)
    {
        IReadOnlyCollection<ClaimNotificationEvent> later = [.. events.Where(e => e.CreatedAt >= after)];
        return later.Count > 0 ? RestoredClaimStateBuilder.Build(existing.ClaimId, later, baseStatus, existing.Player) : null;
    }

    /// <summary>
    /// «Правил руками после восстановления» без журнала применений. Заявка менялась после аварии — это мог
    /// сделать и сам инструмент (применив прошлый план по более старой выгрузке). Поэтому правкой считается,
    /// только меняемый планом атрибут, значение которого в базе не совпадает ни с одной загруженной выгрузкой
    /// этой заявки, ни с состоянием по уведомлениям от какой-либо базы отсчёта (бэкапа или любой из выгрузок):
    /// совпадение значит, что его поставил инструмент. Ручной взнос виден только в уведомлениях, и они
    /// полны, поэтому расхождение по непустому взносу после аварии — всегда правка. Решение — по атрибуту
    /// (<see cref="ClaimRestorePlanRow.AttributesNotByDefault"/>), а не по всей строке.
    /// </summary>
    private static ClaimRestorePlanRow MarkChangedAfterRestore(
        Planning p,
        ClaimRestorePlanRow row,
        ExistingClaimInfo existing,
        IReadOnlyList<ExportedClaimSnapshot> snapshots,
        IReadOnlyCollection<ClaimNotificationEvent> events)
    {
        if (row.Action != ClaimRestoreAction.Change || p.Exports.LostAt is not { } lost || existing.LastUpdated <= lost)
        {
            return row;
        }

        var statuses = new HashSet<ClaimStatus>();
        var responsibles = new HashSet<UserIdentification>();
        var names = new List<string>();
        void AddState(RestoredClaimState? state)
        {
            if (state is null)
            {
                return;
            }
            if (state is { Status: { } status, StatusChangedAt: not null })
            {
                statuses.Add(status);
            }
            if (p.FindMaster(state.ResponsibleMasterName, []) is { } responsible)
            {
                responsibles.Add(responsible);
            }
            if (state.CharacterName is { } name)
            {
                names.Add(name);
            }
        }

        AddState(BuildAfter(existing, events, p.BackupTakenAt, existing.Status));
        foreach (var snapshot in snapshots)
        {
            if (snapshot.Row.Status is { } status)
            {
                statuses.Add(status);
            }
            if (p.FindMaster(snapshot.Row.ResponsibleMasterName, []) is { } responsible)
            {
                responsibles.Add(responsible);
            }
            if (snapshot.Row.Name is { } name)
            {
                names.Add(name);
            }
            AddState(BuildAfter(existing, events, snapshot.ExportedAt, snapshot.Row.Status ?? existing.Status));
        }

        var current = p.Characters.GetValueOrDefault(existing.CharacterId);
        var manual = new List<ClaimRestoreAttributeKind>();
        if (row.Status is not null && !statuses.Contains(existing.Status))
        {
            manual.Add(ClaimRestoreAttributeKind.Status);
        }
        if (row.ResponsibleMaster is not null && !responsibles.Contains(existing.ResponsibleMaster))
        {
            manual.Add(ClaimRestoreAttributeKind.ResponsibleMaster);
        }
        if (row.Character is not null && !names.Any(n => p.IsNameOf(n, current)))
        {
            manual.Add(ClaimRestoreAttributeKind.Character);
        }
        // Пустой взнос правкой не считается.
        if (row.ManualFee is not null && existing.ManualFee is not null)
        {
            manual.Add(ClaimRestoreAttributeKind.ManualFee);
        }
        if (manual.Count == 0)
        {
            return row;
        }

        // Строка в целом — по умолчанию, если в ней осталось что применять по умолчанию.
        var changedCount = new object?[] { row.Status, row.ResponsibleMaster, row.Character, row.ManualFee }.Count(v => v is not null);
        return row with
        {
            AttributesNotByDefault = manual,
            ApplyByDefault = manual.Count < changedCount,
            Warnings = [.. row.Warnings, ClaimRestoreWarning.ChangedAfterRestore],
        };
    }

    /// <summary>Персонаж для потерянной заявки: по первому имени, под которое нашёлся хоть кто-то.</summary>
    private static (CharacterIdentification? Character, IReadOnlyCollection<CharacterIdentification> Candidates) FindLostClaimCharacter(
        Planning p,
        IReadOnlyList<string?> names,
        UserIdentification? player,
        ClaimStatus? status,
        List<ClaimRestoreWarning> warnings)
    {
        var known = names.OfType<string>().ToList();
        if (known.Count == 0)
        {
            warnings.Add(ClaimRestoreWarning.CharacterNotFound);
            return (null, []);
        }

        var matches = known.Select(p.FindCharactersByName).FirstOrDefault(m => m.Count > 0) ?? [];
        if (status is ClaimStatus.Approved or ClaimStatus.CheckedIn && matches.Any(c => c.IsSlot))
        {
            // Одобрение слота создало персонажа с его именем: он и потерян — создать заново.
            warnings.Add(ClaimRestoreWarning.ApprovedOnSlot);
            matches = [.. matches.Where(c => !c.IsSlot)];
        }
        return Choose(p, matches, player, warnings);
    }

    private static (CharacterIdentification? Character, IReadOnlyCollection<CharacterIdentification> Candidates) FindMoveTarget(
        Planning p, ExistingClaimInfo existing, string name, List<ClaimRestoreWarning> warnings)
    {
        var matches = p.FindCharactersByName(name);
        if (matches.Count == 0)
        {
            warnings.Add(ClaimRestoreWarning.MovedCharacterNotFound);
        }
        var (moveTo, candidates) = Choose(p, matches, existing.Player, warnings);
        return (moveTo == existing.CharacterId ? null : moveTo, candidates);
    }

    private static void CheckApprovedOnSlot(
        Planning p, ClaimStatus finalStatus, CharacterIdentification? moveTo, ExistingCharacterInfo? current, List<ClaimRestoreWarning> warnings)
    {
        var finalCharacter = moveTo is { } target ? p.Characters.GetValueOrDefault(target) : current;
        if (finalStatus is ClaimStatus.Approved or ClaimStatus.CheckedIn && finalCharacter?.IsSlot == true)
        {
            warnings.Add(ClaimRestoreWarning.ApprovedOnSlot);
        }
    }

    /// <summary>
    /// В выгрузке только «Итого взнос»: ручной взнос из него не восстановить. Но итог меньше ручного
    /// взноса (если после выгрузки взнос не меняли) — явное расхождение: итог включает ручной взнос
    /// и может быть только больше (проживание).
    /// </summary>
    private static void CheckTotalFee(MasterExportRow row, int? manualFee, RestoredClaimState? afterExport, List<ClaimRestoreWarning> warnings)
    {
        if (row.TotalFee is { } total && manualFee is { } fee && total < fee && afterExport?.ManualFee is null)
        {
            warnings.Add(ClaimRestoreWarning.TotalFeeMismatch);
        }
    }

    /// <summary>
    /// «Уплачено» из выгрузки против суммы одобренных оплат в базе. Только подсказка: оплаты план не трогает,
    /// а после выгрузки могли прийти новые. Заявку из базы сверяем только с выгрузкой свежее бэкапа — в более
    /// старой не видно оплат между ней и бэкапом, и расхождение было бы ложным.
    /// </summary>
    private static FeePaidMismatch? CheckFeePaid(MasterExportRow row, int feePaidInDatabase, List<ClaimRestoreWarning> warnings)
    {
        if (row.FeePaid is not { } exported || exported == feePaidInDatabase)
        {
            return null;
        }
        warnings.Add(ClaimRestoreWarning.FeePaidMismatch);
        return new FeePaidMismatch(exported, feePaidInDatabase);
    }

    private static (DateTimeOffset First, DateTimeOffset Last) Activity(ExportedClaimSnapshot exported, IEnumerable<ClaimNotificationEvent> events)
    {
        List<DateTimeOffset> times = [.. events.Select(e => e.CreatedAt)];
        if (exported.Row.ClaimCreatedAt is { } created)
        {
            times.Add(created);
        }
        if (exported.Row.ClaimUpdatedAt is { } updated)
        {
            times.Add(updated);
        }
        if (times.Count == 0)
        {
            times.Add(exported.ExportedAt);
        }
        return (times.Min(), times.Max());
    }

    private static ClaimRestoreAttributes BuildAttributes(
        Planning p,
        ExistingClaimInfo? existing,
        string? existingCharacterName,
        ExportedClaimSnapshot? exported,
        UserIdentification? exportResponsible,
        RestoredClaimState? state,
        UserIdentification? eventResponsible)
    {
        var backup = p.BackupTakenAt;
        var exportedAt = exported?.ExportedAt ?? default;
        return new ClaimRestoreAttributes(
            RestoreAttribute<ClaimStatus>.Choose(
                existing is null ? null : new(existing.Status, RestoreSourceKind.Database, backup),
                exported?.Row.Status is { } exportStatus ? new(exportStatus, RestoreSourceKind.Export, exportedAt) : null,
                state is { Status: { } eventStatus, StatusChangedAt: { } statusAt } ? new(eventStatus, RestoreSourceKind.Notification, statusAt) : null),
            RestoreAttribute<UserIdentification>.Choose(
                existing is null ? null : new(existing.ResponsibleMaster, RestoreSourceKind.Database, backup),
                exportResponsible is null ? null : new(exportResponsible, RestoreSourceKind.Export, exportedAt),
                eventResponsible is not null && state?.ResponsibleChangedAt is { } responsibleAt
                    ? new(eventResponsible, RestoreSourceKind.Notification, responsibleAt)
                    : null),
            RestoreAttribute<string>.Choose(
                existingCharacterName is null ? null : new(existingCharacterName.Trim(), RestoreSourceKind.Database, backup),
                exported?.Row.Name is { } exportName ? new(exportName.Trim(), RestoreSourceKind.Export, exportedAt) : null,
                state?.CharacterName is { } eventName ? new(eventName.Trim(), RestoreSourceKind.Notification, state.LastActivity) : null));
    }

    internal static bool SameName(string? left, string? right) => left?.Trim() == right?.Trim();

    /// <summary>
    /// Однозначное совпадение — персонаж; несколько — уточняем по игроку персонажа из выгрузки персонажей,
    /// иначе кандидаты и пометка.
    /// </summary>
    private static (CharacterIdentification? Character, IReadOnlyCollection<CharacterIdentification> Candidates) Choose(
        Planning p, IReadOnlyList<ExistingCharacterInfo> matches, UserIdentification? player, List<ClaimRestoreWarning> warnings)
    {
        switch (matches.Count)
        {
            case 0:
                return (null, []);
            case 1:
                return (matches[0].CharacterId, []);
            default:
                if (player is not null)
                {
                    var players = matches.Where(c => p.Exports.CharacterPlayers.GetValueOrDefault(c.CharacterId) == player).ToList();
                    if (players.Count == 1)
                    {
                        return (players[0].CharacterId, []);
                    }
                }
                warnings.Add(ClaimRestoreWarning.CharacterAmbiguous);
                return (null, [.. matches.Select(c => c.CharacterId)]);
        }
    }

    private static UserIdentification? FindUserByEmail(ClaimExportContext exports, string? email)
        => string.IsNullOrWhiteSpace(email) ? null : exports.UsersByEmail.GetValueOrDefault(email.Trim());

    private static List<ClaimRestoreWarning> ConvertWarnings(IEnumerable<RestoredClaimWarning> warnings)
        => [.. warnings.Select(w => w switch
        {
            RestoredClaimWarning.PlayerNotFound => ClaimRestoreWarning.PlayerNotFound,
            RestoredClaimWarning.PlayerAmbiguous => ClaimRestoreWarning.PlayerAmbiguous,
            _ => throw new ArgumentOutOfRangeException(nameof(warnings), w, null),
        })];
}
