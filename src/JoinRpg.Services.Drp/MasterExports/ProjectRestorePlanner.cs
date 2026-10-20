using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>
/// Строит план восстановления проекта по выгрузкам мастера (xlsx/csv) и уведомлениям (DRP, #5529).
/// Чистый: ничего не читает и не пишет, только сверяет переданное.
/// </summary>
/// <remarks>
/// <para>По каждому атрибуту берётся самое свежее значение. Выгрузки упорядочиваются по дате.</para>
/// <list type="bullet">
/// <item>Группы, состав групп и id персонажей — из самой свежей выгрузки персонажей, где персонаж есть.</item>
/// <item>Поля персонажа — по каждому полю отдельно: самая свежая выгрузка, где колонка поля сопоставлена.
/// Источник — выгрузки персонажей и выгрузки заявок (у заявки, одобренной на момент выгрузки, колонки
/// полей персонажа — это поля персонажа).</item>
/// <item>Заявки — <see cref="ClaimRestorePlanner"/>: база отсчёта каждой заявки — самый свежий снимок из
/// восстановленной базы и выгрузки заявок, поверх него — события из уведомлений после него.</item>
/// <item>Если значение взято из выгрузки старше бэкапа, база свежее — это изменение существующей сущности
/// по умолчанию не применяется.</item>
/// <item>«Правил руками» — без журнала применений, по полю (атрибуту): сущность менялась после аварии,
/// и непустое меняемое значение в базе не совпадает ни с одной загруженной выгрузкой. Совпадение с какой-то
/// выгрузкой значит, что его поставил сам инструмент (например, применив прошлый план по более старой
/// выгрузке), — тогда новая выгрузка применяется по умолчанию (<see cref="ProjectRestoreWarning.ChangedAfterRestore"/>).
/// Строка остаётся по умолчанию, если в ней есть что применять по умолчанию.</item>
/// <item>Порядок применения — см. <see cref="ProjectRestorePlan"/>: изменения существующих персонажей идут
/// после заявок.</item>
/// </list>
/// </remarks>
public static class ProjectRestorePlanner
{
    private sealed record MappedExport(
        DatedMasterExport Dated,
        IReadOnlyList<MasterExportColumnMapping> Columns,
        IReadOnlyDictionary<int, MasterExportRow> Rows)
    {
        public DateTimeOffset ExportedAt => Dated.ExportedAt;
        public ParsedMasterExport Export => Dated.Export;
    }

    /// <summary>Значение поля из одной строки одной выгрузки.</summary>
    private sealed record FieldCandidate(
        ProjectFieldInfo Field,
        string? StoredValue,
        bool Truncated,
        bool ApplyByDefault,
        IReadOnlyList<MasterExportFieldWarning> Warnings,
        DateTimeOffset ExportedAt,
        MasterExportKind Kind);

    /// <summary>Персонаж из выгрузки персонажей — до сверки полей.</summary>
    private sealed record CharacterSkeleton(
        CharacterIdentification CharacterId,
        ProjectRestoreAction Action,
        string? Name,
        MasterExportRow Row,
        DateTimeOffset ExportedAt,
        IReadOnlyList<CharacterGroupIdentification> TargetGroups,
        bool ApplyByDefault,
        List<ProjectRestoreWarning> Warnings);

    public static ProjectRestorePlan Plan(ProjectRestoreSource source)
    {
        var project = source.Project;
        var notes = new List<ProjectRestoreNote>();

        var exportInfos = new List<MasterExportPlanInfo>();
        var characterExports = new List<MappedExport>();
        var claimExports = new List<MappedExport>();
        foreach (var dated in source.Exports.OrderBy(e => e.ExportedAt))
        {
            var columns = MasterExportFieldMapper.MapColumns(project, dated.Export.OtherHeaders);
            var used = dated.Export.ProjectId == project.ProjectId.Value;
            exportInfos.Add(new MasterExportPlanInfo(dated.Export.Kind, dated.ExportedAt, used, columns));
            if (!used)
            {
                notes.Add(new ProjectRestoreNote(ProjectRestoreWarning.ExportFromOtherProject, dated.ExportedAt, null));
                continue;
            }
            var isCharacters = dated.Export.Kind == MasterExportKind.Characters;
            var rows = IndexRows(dated, r => isCharacters ? r.CharacterId : r.ClaimId, notes);
            (isCharacters ? characterExports : claimExports).Add(new MappedExport(dated, columns, rows));
        }

        var usersByEmail = new Dictionary<string, UserIdentification>(StringComparer.OrdinalIgnoreCase);
        foreach (var (email, user) in source.UsersByEmail)
        {
            usersByEmail.TryAdd(email.Trim(), user);
        }

        var (groups, usableGroups) = PlanGroups(source, characterExports);
        var skeletons = PlanCharacterSkeletons(source, characterExports, usableGroups);
        var claims = PlanClaims(source, claimExports, skeletons, usersByEmail);
        // Шаблоны — и из базы, и те, что план создаёт по выгрузке: принятая заявка на них уйдёт на своего персонажа.
        var slots = source.Notifications.Characters.Where(c => c.IsSlot).Select(c => c.CharacterId)
            .Concat(skeletons
                .Where(s => s.Action == ProjectRestoreAction.Create && s.Row.CharacterType?.Type == CharacterType.Slot)
                .Select(s => s.CharacterId))
            .ToHashSet();
        var (characters, plannedValues) = PlanCharacterFields(source, characterExports, claimExports, skeletons, claims, usableGroups, slots);
        var claimFields = PlanClaimFields(source, claimExports, claims, plannedValues, slots);

        // Существующих персонажей меняем после заявок: одобрение заявки переносит её слой в персонажа.
        var existing = source.Notifications.Characters.Select(c => c.CharacterId).ToHashSet();
        return new ProjectRestorePlan(
            exportInfos,
            groups,
            [.. characters.Where(c => !existing.Contains(c.CharacterId))],
            claims,
            [.. characters.Where(c => existing.Contains(c.CharacterId))],
            claimFields,
            notes);
    }

    /// <summary>Строки выгрузки по id. Строки без id, со ссылками в чужой проект и повторы — в пометки.</summary>
    private static Dictionary<int, MasterExportRow> IndexRows(DatedMasterExport dated, Func<MasterExportRow, int?> getId, List<ProjectRestoreNote> notes)
    {
        var rows = new Dictionary<int, MasterExportRow>();
        foreach (var row in dated.Export.Rows)
        {
            ProjectRestoreWarning? skip = getId(row) is not { } id ? ProjectRestoreWarning.RowWithoutId
                : row.Warnings.Contains(MasterExportWarning.ProjectIdMismatch) ? ProjectRestoreWarning.RowFromOtherProject
                : !rows.TryAdd(id, row) ? ProjectRestoreWarning.DuplicateRow
                : null;
            if (skip is { } warning)
            {
                notes.Add(new ProjectRestoreNote(warning, dated.ExportedAt, row.RowNumber));
            }
        }
        return rows;
    }

    private static (IReadOnlyList<GroupRestorePlanRow> Rows, HashSet<int> Usable) PlanGroups(
        ProjectRestoreSource source, IReadOnlyList<MappedExport> characterExports)
    {
        var projectId = source.Project.ProjectId;
        // Имя — из самой свежей выгрузки, где у группы есть имя; дата — самой свежей, где группа есть.
        var exported = new Dictionary<int, (string? Name, DateTimeOffset ExportedAt)>();
        foreach (var export in characterExports.Reverse())
        {
            foreach (var group in export.Rows.Values.SelectMany(r => r.Groups))
            {
                var name = string.IsNullOrWhiteSpace(group.Name) ? null : group.Name.Trim();
                if (!exported.TryGetValue(group.CharacterGroupId, out var known))
                {
                    exported[group.CharacterGroupId] = (name, export.ExportedAt);
                }
                else if (known.Name is null && name is not null)
                {
                    exported[group.CharacterGroupId] = (name, known.ExportedAt);
                }
            }
        }

        var existing = source.Groups.ToDictionary(g => g.GroupId);
        var taken = source.GroupIdsTakenByOtherProjects.ToHashSet();
        // Ставить персонажам можно только группы, которые точно будут: живые в базе и создаваемые по умолчанию.
        var usable = new HashSet<int>();
        var rows = new List<GroupRestorePlanRow>();
        foreach (var (id, (name, exportedAt)) in exported.OrderBy(g => g.Key))
        {
            var groupId = new CharacterGroupIdentification(projectId, id);
            var warnings = new List<ProjectRestoreWarning>();
            if (existing.TryGetValue(groupId, out var current))
            {
                if (current.IsActive)
                {
                    usable.Add(id);
                }
                else
                {
                    warnings.Add(ProjectRestoreWarning.GroupInactive);
                }
                if (name is not null && name != current.Name.Trim())
                {
                    warnings.Add(ProjectRestoreWarning.GroupNameDiffers);
                }
                rows.Add(new GroupRestorePlanRow(groupId, ProjectRestoreAction.Nothing, name, current.Name, exportedAt, true, warnings));
            }
            else if (taken.Contains(id))
            {
                warnings.Add(ProjectRestoreWarning.GroupIdTakenByOtherProject);
                rows.Add(new GroupRestorePlanRow(groupId, ProjectRestoreAction.Nothing, name, null, exportedAt, false, warnings));
            }
            else
            {
                if (name is null)
                {
                    warnings.Add(ProjectRestoreWarning.GroupNameUnknown);
                }
                if (exportedAt < source.Notifications.BackupTakenAt)
                {
                    warnings.Add(ProjectRestoreWarning.ExportOlderThanBackup);
                }
                var apply = warnings.Count == 0;
                if (apply)
                {
                    usable.Add(id);
                }
                rows.Add(new GroupRestorePlanRow(groupId, ProjectRestoreAction.Create, name, null, exportedAt, apply, warnings));
            }
        }
        return (rows, usable);
    }

    /// <summary>Что делать с каждым персонажем из выгрузок персонажей — без полей (их сверяет <see cref="PlanCharacterFields"/>).</summary>
    private static List<CharacterSkeleton> PlanCharacterSkeletons(
        ProjectRestoreSource source, IReadOnlyList<MappedExport> characterExports, HashSet<int> usableGroups)
    {
        var projectId = source.Project.ProjectId;
        var existing = source.Notifications.Characters.ToDictionary(c => c.CharacterId);
        var taken = source.CharacterIdsTakenByOtherProjects.ToHashSet();
        var latestExportAt = characterExports.Count > 0 ? characterExports[^1].ExportedAt : default;

        var latest = new Dictionary<int, (MappedExport Export, MasterExportRow Row)>();
        foreach (var export in characterExports.Reverse())
        {
            foreach (var (id, row) in export.Rows)
            {
                latest.TryAdd(id, (export, row));
            }
        }

        var result = new List<CharacterSkeleton>();
        foreach (var (id, (export, exportRow)) in latest.OrderBy(r => r.Key))
        {
            var row = exportRow;
            var characterId = new CharacterIdentification(projectId, id);
            var warnings = new List<ProjectRestoreWarning>();
            var name = string.IsNullOrWhiteSpace(row.Name) ? null : row.Name.Trim();
            var missingInNewer = export.ExportedAt < latestExportAt;
            if (missingInNewer)
            {
                warnings.Add(ProjectRestoreWarning.MissingInNewerExport);
            }

            var exportGroups = row.Groups.Select(g => g.CharacterGroupId).Distinct().ToList();
            List<CharacterGroupIdentification> targetGroups = [.. exportGroups
                .Where(usableGroups.Contains)
                .Select(g => new CharacterGroupIdentification(projectId, g))];
            if (targetGroups.Count < exportGroups.Count)
            {
                warnings.Add(ProjectRestoreWarning.GroupSkipped);
            }

            ProjectRestoreAction action;
            bool apply;
            if (existing.TryGetValue(characterId, out var current))
            {
                // Change или Nothing — решит сверка полей и групп.
                action = current.IsActive ? ProjectRestoreAction.Change : ProjectRestoreAction.Nothing;
                apply = current.IsActive;
                if (!current.IsActive)
                {
                    warnings.Add(ProjectRestoreWarning.CharacterInactive);
                }
                else if (row.CharacterType is { } exportType && current.CharacterType is { } currentType && exportType.Type != currentType)
                {
                    // Тип не меняем: шаблон с заявками или персонаж с заявками — смена типа не механическая.
                    warnings.Add(ProjectRestoreWarning.CharacterTypeDiffers);
                }
            }
            else if (taken.Contains(id))
            {
                warnings.Add(ProjectRestoreWarning.CharacterIdTakenByOtherProject);
                action = ProjectRestoreAction.Nothing;
                apply = false;
            }
            else
            {
                action = ProjectRestoreAction.Create;
                if (source.NewCharacterIsSlot.TryGetValue(characterId, out var isSlot))
                {
                    // Выбор мастера — дальше по плану (привязка и поля заявок) персонаж такой, каким его создадут.
                    row = row with { CharacterType = WithSlotChoice(row.CharacterType, isSlot) };
                }
                var olderThanBackup = export.ExportedAt < source.Notifications.BackupTakenAt;
                if (name is null)
                {
                    warnings.Add(ProjectRestoreWarning.CharacterNameUnknown);
                }
                if (olderThanBackup)
                {
                    warnings.Add(ProjectRestoreWarning.ExportOlderThanBackup);
                }
                apply = name is not null && !missingInNewer && !olderThanBackup;
            }
            result.Add(new CharacterSkeleton(characterId, action, name, row, export.ExportedAt, targetGroups, apply, warnings));
        }
        return result;
    }

    /// <summary>
    /// Тип нового персонажа с выбором мастера «шаблон / не шаблон» (#5733). Горячесть — из выгрузки; снятый
    /// шаблон становится персонажем игрока, NPC и персонаж игрока остаются собой.
    /// </summary>
    private static MasterExportCharacterType? WithSlotChoice(MasterExportCharacterType? exported, bool isSlot)
    {
        if (isSlot)
        {
            return new MasterExportCharacterType(CharacterType.Slot, exported?.IsHot ?? false);
        }
        return exported?.Type == CharacterType.Slot ? exported with { Type = CharacterType.Player } : exported;
    }

    private static IReadOnlyList<ClaimRestorePlanRow> PlanClaims(
        ProjectRestoreSource source,
        IReadOnlyList<MappedExport> claimExports,
        IReadOnlyList<CharacterSkeleton> skeletons,
        IReadOnlyDictionary<string, UserIdentification> usersByEmail)
    {
        var projectId = source.Project.ProjectId;
        var notifications = source.Notifications;

        var characterPlayers = new Dictionary<CharacterIdentification, UserIdentification>();
        var characterNames = new Dictionary<CharacterIdentification, string>();
        foreach (var skeleton in skeletons)
        {
            if (skeleton.Row.PlayerEmail is { } email && usersByEmail.GetValueOrDefault(email.Trim()) is { } player)
            {
                characterPlayers[skeleton.CharacterId] = player;
            }
            if (skeleton.Name is { } name)
            {
                characterNames[skeleton.CharacterId] = name;
            }
        }

        var snapshots = new Dictionary<ClaimIdentification, List<ExportedClaimSnapshot>>();
        foreach (var export in claimExports)
        {
            foreach (var (id, row) in export.Rows)
            {
                var claimId = new ClaimIdentification(projectId, id);
                if (!snapshots.TryGetValue(claimId, out var list))
                {
                    snapshots[claimId] = list = [];
                }
                list.Add(new ExportedClaimSnapshot(row, export.ExportedAt));
            }
        }

        var context = new ClaimExportContext(
            snapshots.ToDictionary(s => s.Key, s => (IReadOnlyList<ExportedClaimSnapshot>)s.Value),
            usersByEmail,
            characterPlayers,
            characterNames,
            source.LostAt);

        // Кандидаты для привязки заявок по имени — персонажи базы и те, кого план создаёт по умолчанию.
        IReadOnlyCollection<ExistingCharacterInfo> candidates = [
            .. notifications.Characters,
            .. skeletons
                .Where(c => c is { Action: ProjectRestoreAction.Create, ApplyByDefault: true, Name: not null })
                .Select(c => new ExistingCharacterInfo(
                    c.CharacterId,
                    c.Name!,
                    IsActive: true,
                    IsSlot: c.Row.CharacterType?.Type == CharacterType.Slot,
                    FindMaster(notifications.Masters, c.Row.ResponsibleMasterName) ?? notifications.DefaultResponsibleMaster)
                {
                    CharacterType = (c.Row.CharacterType ?? MasterExportCharacterType.Player).Type,
                }),
        ];

        return ClaimRestorePlanner.Plan(notifications with { Characters = candidates }, context);
    }

    /// <summary>
    /// Персонаж, на котором заявка окажется по плану; null — не определён (создаётся новый или нужен выбор).
    /// Принятая заявка на слоте окажется на своём персонаже из него (#5645) — его ещё нет, тоже null.
    /// </summary>
    private static CharacterIdentification? FinalCharacter(
        ClaimRestorePlanRow? row, ExistingClaimInfo? existing, IReadOnlySet<CharacterIdentification> slots)
        => PlannedCharacter(row, existing) is { } character && !CreatesCharacterFromSlot(row, existing, slots) ? character : null;

    private static CharacterIdentification? PlannedCharacter(ClaimRestorePlanRow? row, ExistingClaimInfo? existing)
        => row?.Character ?? (row?.Action == ClaimRestoreAction.Create ? null : existing?.CharacterId);

    /// <summary>Принятая по итогу заявка окажется на слоте: восстановление создаст ей персонажа из него.</summary>
    private static bool CreatesCharacterFromSlot(
        ClaimRestorePlanRow? row, ExistingClaimInfo? existing, IReadOnlySet<CharacterIdentification> slots)
        => PlannedCharacter(row, existing) is { } character
            && slots.Contains(character)
            && IsApproved(FinalStatus(row, existing));

    private static ClaimStatus? FinalStatus(ClaimRestorePlanRow? row, ExistingClaimInfo? existing) => row?.Status ?? existing?.Status;

    private static bool IsApproved(ClaimStatus? status) => status is ClaimStatus.Approved or ClaimStatus.CheckedIn;

    private static (List<CharacterRestorePlanRow> Rows, Dictionary<CharacterIdentification, Dictionary<ProjectFieldIdentification, string?>> PlannedValues) PlanCharacterFields(
        ProjectRestoreSource source,
        IReadOnlyList<MappedExport> characterExports,
        IReadOnlyList<MappedExport> claimExports,
        IReadOnlyList<CharacterSkeleton> skeletons,
        IReadOnlyList<ClaimRestorePlanRow> claims,
        HashSet<int> usableGroups,
        IReadOnlySet<CharacterIdentification> slots)
    {
        var project = source.Project;
        var projectId = project.ProjectId;
        var backupAt = source.Notifications.BackupTakenAt;
        var existing = source.Notifications.Characters.ToDictionary(c => c.CharacterId);
        var existingClaims = source.Notifications.Claims.ToDictionary(c => c.ClaimId);
        var claimRows = claims.ToDictionary(c => c.ClaimId);
        var skeletonsById = skeletons.ToDictionary(s => s.CharacterId);
        static bool IsCharacterField(ProjectFieldInfo f) => f.BoundTo == FieldBoundTo.Character;

        // Значения полей персонажа из всех выгрузок, где он есть, и все его наборы групп.
        var candidates = new Dictionary<CharacterIdentification, List<FieldCandidate>>();
        var groupSets = new Dictionary<CharacterIdentification, List<HashSet<CharacterGroupIdentification>>>();
        void AddCandidates(CharacterIdentification characterId, List<FieldCandidate> values)
        {
            // Без значений персонаж не заводится: у него не было бы ни строки выгрузки персонажей, ни даты.
            if (values.Count == 0)
            {
                return;
            }
            if (!candidates.TryGetValue(characterId, out var list))
            {
                candidates[characterId] = list = [];
            }
            list.AddRange(values);
        }

        foreach (var export in characterExports)
        {
            foreach (var (id, row) in export.Rows)
            {
                var characterId = new CharacterIdentification(projectId, id);
                AddCandidates(characterId, MapFields(project, export, row, IsCharacterField));
                if (!groupSets.TryGetValue(characterId, out var sets))
                {
                    groupSets[characterId] = sets = [];
                }
                // И как в выгрузке, и как его поставил бы инструмент — только с группами, которые можно ставить.
                sets.Add([.. row.Groups.Select(g => new CharacterGroupIdentification(projectId, g.CharacterGroupId))]);
                sets.Add([.. row.Groups.Where(g => usableGroups.Contains(g.CharacterGroupId)).Select(g => new CharacterGroupIdentification(projectId, g.CharacterGroupId))]);
            }
        }

        // У заявки, одобренной на момент выгрузки, колонки полей персонажа — это поля её персонажа.
        foreach (var export in claimExports)
        {
            foreach (var (id, row) in export.Rows)
            {
                if (!IsApproved(row.Status))
                {
                    continue;
                }
                var claimId = new ClaimIdentification(projectId, id);
                if (FinalCharacter(claimRows.GetValueOrDefault(claimId), existingClaims.GetValueOrDefault(claimId), slots) is not { } characterId)
                {
                    continue;
                }
                // Другое имя — заявку перемещали после выгрузки, колонки относились к другому персонажу.
                var characterName = skeletonsById.GetValueOrDefault(characterId)?.Name ?? existing.GetValueOrDefault(characterId)?.Name;
                if (!ClaimRestorePlanner.SameName(characterName, row.Name))
                {
                    continue;
                }
                AddCandidates(characterId, MapFields(project, export, row, IsCharacterField));
            }
        }

        var rows = new List<CharacterRestorePlanRow>();
        var planned = new Dictionary<CharacterIdentification, Dictionary<ProjectFieldIdentification, string?>>();
        var ids = skeletons.Select(s => s.CharacterId)
            .Concat(candidates.Keys.Where(c => existing.GetValueOrDefault(c)?.IsActive == true))
            .Distinct()
            .OrderBy(c => c.CharacterId);
        foreach (var characterId in ids)
        {
            var skeleton = skeletonsById.GetValueOrDefault(characterId);
            var current = existing.GetValueOrDefault(characterId);
            var all = candidates.GetValueOrDefault(characterId) ?? [];
            var chosen = Freshest(all);
            var warnings = skeleton?.Warnings ?? [];
            var exportedAt = skeleton?.ExportedAt ?? all.Max(c => c.ExportedAt);

            if (skeleton is { Action: ProjectRestoreAction.Nothing })
            {
                rows.Add(new CharacterRestorePlanRow(characterId, ProjectRestoreAction.Nothing, skeleton.Name, null,
                    current?.Groups ?? [], [], null, exportedAt, false, warnings)
                {
                    ExportCharacterType = skeleton.Row.CharacterType,
                });
                continue;
            }

            if (current is null)
            {
                // Создание: все непустые значения.
                var created = Changes(chosen, _ => null);
                if (skeleton!.ApplyByDefault)
                {
                    planned[characterId] = created.Where(f => f.ApplyByDefault).ToDictionary(f => f.Field, f => f.Value);
                }
                rows.Add(new CharacterRestorePlanRow(
                    characterId,
                    ProjectRestoreAction.Create,
                    skeleton.Name,
                    skeleton.TargetGroups,
                    [],
                    created,
                    FindMaster(source.Notifications.Masters, skeleton.Row.ResponsibleMasterName),
                    exportedAt,
                    skeleton.ApplyByDefault,
                    warnings)
                {
                    ExportCharacterType = skeleton.Row.CharacterType,
                });
                continue;
            }

            var nameFieldMapped = project.CharacterNameField is { } nameField && chosen.Any(v => v.Field.Id == nameField.Id);
            if (skeleton?.Name is { } exportName && exportName != current.Name.Trim() && !nameFieldMapped)
            {
                warnings.Add(ProjectRestoreWarning.CharacterNameDiffers);
            }

            var changedAfterLoss = current.UpdatedAt > source.LostAt;
            var fields = Changes(chosen, f => current.FieldLayer?.GetValue(f))
                .Select(f => MarkField(f, all, changedAfterLoss, backupAt))
                .ToList();

            var currentGroups = current.Groups.ToHashSet();
            var groupsChange = skeleton is null || currentGroups.SetEquals(skeleton.TargetGroups) ? null : skeleton.TargetGroups;
            var groupsApply = true;
            if (groupsChange is not null)
            {
                // Набор в базе есть в какой-то выгрузке (как есть или как его поставил бы инструмент) — его
                // поставил инструмент. Пустой набор правкой не считается.
                if (changedAfterLoss && currentGroups.Count > 0
                    && !(groupSets.GetValueOrDefault(characterId) ?? []).Any(s => s.SetEquals(currentGroups)))
                {
                    warnings.Add(ProjectRestoreWarning.ChangedAfterRestore);
                    groupsApply = false;
                }
                if (skeleton!.ExportedAt < backupAt)
                {
                    warnings.Add(ProjectRestoreWarning.ExportOlderThanBackup);
                    groupsApply = false;
                }
            }
            warnings.AddRange(fields.SelectMany(f => f.RestoreWarnings));

            var changed = fields.Count > 0 || groupsChange is not null;
            // Строка — по умолчанию, если в ней есть что применять по умолчанию.
            var apply = !changed || fields.Any(f => f.ApplyByDefault) || (groupsChange is not null && groupsApply);

            // Плановые значения — база с изменениями, которые применятся по умолчанию.
            var values = ValuesOf(current.FieldLayer);
            foreach (var field in fields.Where(f => f.ApplyByDefault))
            {
                values[field.Field] = field.Value;
            }
            planned[characterId] = values;

            rows.Add(new CharacterRestorePlanRow(
                characterId,
                changed ? ProjectRestoreAction.Change : ProjectRestoreAction.Nothing,
                skeleton?.Name ?? current.Name,
                groupsChange,
                current.Groups,
                fields,
                null,
                exportedAt,
                apply,
                [.. warnings.Distinct()])
            {
                GroupsApplyByDefault = groupsApply,
                ExportCharacterType = skeleton?.Row.CharacterType,
            });
        }
        return (rows, planned);
    }

    private static List<ClaimFieldsRestorePlanRow> PlanClaimFields(
        ProjectRestoreSource source,
        IReadOnlyList<MappedExport> claimExports,
        IReadOnlyList<ClaimRestorePlanRow> claims,
        IReadOnlyDictionary<CharacterIdentification, Dictionary<ProjectFieldIdentification, string?>> plannedCharacterValues,
        IReadOnlySet<CharacterIdentification> slots)
    {
        var project = source.Project;
        var projectId = project.ProjectId;
        var existingClaims = source.Notifications.Claims.ToDictionary(c => c.ClaimId);
        var existingCharacters = source.Notifications.Characters.ToDictionary(c => c.CharacterId);
        var claimRows = claims.ToDictionary(c => c.ClaimId);

        var rows = new List<ClaimFieldsRestorePlanRow>();
        foreach (var id in claimExports.SelectMany(e => e.Rows.Keys).Distinct().Order())
        {
            var claimId = new ClaimIdentification(projectId, id);
            var existing = existingClaims.GetValueOrDefault(claimId);
            var claimRow = claimRows.GetValueOrDefault(claimId);

            var finalStatus = FinalStatus(claimRow, existing);
            var character = FinalCharacter(claimRow, existing, slots);
            // Одобренная по итогу потерянная заявка без персонажа сама создаёт его при одобрении (как и принятая
            // на слот — персонажа из него): поля персонажа из её выгрузок идут в неё, домен у одобренной запишет
            // их в персонажа.
            var createsCharacter = (claimRow is { Action: ClaimRestoreAction.Create } && character is null && IsApproved(finalStatus))
                || CreatesCharacterFromSlot(claimRow, existing, slots);
            // Иначе поля персонажа в слое заявки — только пока она не одобрена по итогу (по плану): у одобренной
            // домен их хранит в персонаже. И только из выгрузок новее последней, где заявка была одобрена: в той
            // выгрузке колонки показывали поля персонажа, а не предложения игрока.
            var holdsSuggestions = finalStatus is not null && !IsApproved(finalStatus);
            var lastApprovedAt = claimExports
                .Where(e => e.Rows.GetValueOrDefault(id) is { } r && IsApproved(r.Status))
                .Select(e => (DateTimeOffset?)e.ExportedAt)
                .Max();

            var all = new List<FieldCandidate>();
            foreach (var export in claimExports)
            {
                if (export.Rows.GetValueOrDefault(id) is not { } row)
                {
                    continue;
                }
                var showsSuggestions = holdsSuggestions && row.Status is { } status && !IsApproved(status)
                    && (lastApprovedAt is null || export.ExportedAt > lastApprovedAt);
                var takesCharacterFields = createsCharacter || showsSuggestions;
                all.AddRange(MapFields(project, export, row,
                    f => f.BoundTo == FieldBoundTo.Claim || (takesCharacterFields && f.BoundTo == FieldBoundTo.Character)));
            }

            // Значения персонажа — по плану персонажей, а если его там нет — из базы.
            IReadOnlyDictionary<ProjectFieldIdentification, string?> characterValues = character is null
                ? new Dictionary<ProjectFieldIdentification, string?>()
                : plannedCharacterValues.GetValueOrDefault(character)
                    ?? ValuesOf(existingCharacters.GetValueOrDefault(character)?.FieldLayer);

            var changedAfterLoss = existing is not null && existing.LastUpdated > source.LostAt;
            var fields = new List<FieldRestoreChange>();
            foreach (var value in Freshest(all))
            {
                var fieldId = value.Field.Id;
                var layerValue = existing?.FieldLayer?.GetValue(value.Field);
                // Предложение выгрузка показывает как есть, а без него — значение персонажа (по плану персонажей).
                // Значение, равное персонажу, при текущем предложении — это очистка предложения: домен выбросит
                // значение, равное персонажу, и выгрузка из базы покажет то же самое.
                var shown = value.Field.BoundTo == FieldBoundTo.Character && !createsCharacter
                    ? Normalize(layerValue) ?? characterValues.GetValueOrDefault(fieldId)
                    : layerValue;
                if (SameValue(value, shown))
                {
                    continue;
                }
                fields.Add(MarkField(ToChange(value, layerValue), all, changedAfterLoss, existing is null ? DateTimeOffset.MinValue : source.Notifications.BackupTakenAt));
            }
            if (fields.Count == 0)
            {
                continue;
            }

            List<ProjectRestoreWarning> warnings = [.. fields.SelectMany(f => f.RestoreWarnings).Distinct()];
            var needsDecision = claimRow is { Action: ClaimRestoreAction.Create } && (!claimRow.ApplyByDefault || claimRow.NeedsManualChoice);
            if (needsDecision)
            {
                warnings.Add(ProjectRestoreWarning.ClaimNeedsDecision);
            }
            var apply = !needsDecision && fields.Any(f => f.ApplyByDefault);
            rows.Add(new ClaimFieldsRestorePlanRow(claimId, fields, fields.Max(f => f.ExportedAt), apply, warnings));
        }
        return rows;
    }

    /// <summary>Значения сопоставленных колонок строки — по одному на поле (повтор колонки поля пропускается).</summary>
    private static List<FieldCandidate> MapFields(ProjectInfo project, MappedExport export, MasterExportRow row, Func<ProjectFieldInfo, bool> include)
    {
        var seen = new HashSet<ProjectFieldIdentification>();
        var result = new List<FieldCandidate>();
        for (var i = 0; i < export.Columns.Count; i++)
        {
            var mapping = export.Columns[i];
            if (mapping.Field is not { } fieldId || !seen.Add(fieldId))
            {
                continue;
            }
            var field = project.GetFieldById(fieldId);
            if (!include(field))
            {
                continue;
            }
            var cell = i < row.OtherCells.Count && row.OtherCells[i].Header == mapping.Header
                ? row.OtherCells[i]
                : row.OtherCells.FirstOrDefault(c => c.Header == mapping.Header);
            if (cell is null)
            {
                continue;
            }
            var value = MasterExportFieldMapper.MapValue(field, cell.Value, cell.Truncated);
            IReadOnlyList<MasterExportFieldWarning> warnings = [.. mapping.Warnings.Concat(value.Warnings).Distinct()];
            var apply = value.ApplyByDefault && mapping.Warnings.Count == 0;
            result.Add(new FieldCandidate(field, value.StoredValue, cell.Truncated, apply, warnings, export.ExportedAt, export.Export.Kind));
        }
        return result;
    }

    /// <summary>
    /// По каждому полю — значение из самой свежей выгрузки, где колонка поля сопоставлена. При равной дате
    /// выгрузка персонажей важнее выгрузки заявок — она основной источник полей персонажа.
    /// </summary>
    private static List<FieldCandidate> Freshest(IEnumerable<FieldCandidate> candidates)
        => [.. candidates
            .Select((c, i) => (c, i))
            .GroupBy(x => x.c.Field.Id)
            .Select(g => g
                .OrderBy(x => x.c.ExportedAt)
                .ThenBy(x => x.c.Kind == MasterExportKind.Characters)
                .ThenBy(x => x.i)
                .Last().c)];

    /// <param name="current">Значение поля в базе сейчас.</param>
    private static List<FieldRestoreChange> Changes(IEnumerable<FieldCandidate> values, Func<ProjectFieldInfo, string?> current)
        => [.. values
            .Select(v => (v, Current: current(v.Field)))
            .Where(x => !SameValue(x.v, x.Current))
            .Select(x => ToChange(x.v, x.Current))];

    /// <summary>Хранимые значения слоя по полям; слоя нет — пусто.</summary>
    private static Dictionary<ProjectFieldIdentification, string?> ValuesOf(FieldLayerContainer? layer)
        => layer?.LayerData.ToDictionary(v => v.Key, v => v.Value.Value) ?? [];

    private static FieldRestoreChange ToChange(FieldCandidate value, string? current)
        => new(value.Field.Id, value.StoredValue, current, value.ExportedAt, value.Kind, value.ApplyByDefault, value.Warnings, []);

    /// <summary>
    /// Пометки слияния по одному полю существующей сущности; с любой из них поле по умолчанию не применяется.
    /// <list type="bullet">
    /// <item>«Правил руками» (решение пользователя — по полю, без журнала): сущность менялась после аварии,
    /// а непустое значение в базе не совпадает ни с одной загруженной выгрузкой. Совпадение значит, что его
    /// поставил инструмент; пустое значение правкой не считается. Значение, поставленное из выгрузки, которую
    /// мастер не загрузил снова, будет помечено ложно — мастер видит текущее и новое и решает сам.</item>
    /// <item>Значение из выгрузки старше бэкапа — база свежее.</item>
    /// </list>
    /// </summary>
    private static FieldRestoreChange MarkField(
        FieldRestoreChange change, IReadOnlyCollection<FieldCandidate> all, bool changedAfterLoss, DateTimeOffset backupAt)
    {
        var warnings = new List<ProjectRestoreWarning>();
        if (changedAfterLoss
            && Normalize(change.CurrentValue) is not null
            && !all.Any(c => c.Field.Id == change.Field && SameValue(c, change.CurrentValue)))
        {
            warnings.Add(ProjectRestoreWarning.ChangedAfterRestore);
        }
        if (change.ExportedAt < backupAt)
        {
            warnings.Add(ProjectRestoreWarning.ExportOlderThanBackup);
        }
        return warnings.Count == 0 ? change : change with { ApplyByDefault = false, RestoreWarnings = warnings };
    }

    /// <summary>
    /// Совпадает ли значение из выгрузки с хранимым. Пустое и отсутствующее — одно и то же; переводы строк
    /// сравниваются без учёта вида (CRLF/LF); варианты списка — как множество; обрезанное выгрузкой
    /// значение совпадает, если таким его и делает выгрузка из базы.
    /// </summary>
    private static bool SameValue(FieldCandidate value, string? current)
    {
        var stored = Normalize(value.StoredValue);
        var existing = Normalize(current);
        if (stored == existing)
        {
            return true;
        }
        if (stored is null || existing is null)
        {
            return false;
        }
        if (value.Field.HasValueList)
        {
            return SplitIds(stored).SetEquals(SplitIds(existing));
        }
        return existing == stored || (value.Truncated && existing.StartsWith(stored, StringComparison.Ordinal));
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.ReplaceLineEndings("\n");

    private static HashSet<string> SplitIds(string value)
        => [.. value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)];

    private static UserIdentification? FindMaster(IEnumerable<ProjectMasterName> masters, string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }
        var matches = masters.Where(m => m.DisplayName.Trim() == name.Trim()).ToList();
        return matches.Count == 1 ? matches[0].UserId : null;
    }
}
