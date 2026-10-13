using JoinRpg.Common.PrimitiveTypes.Users;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.Services.Drp.ClaimNotifications;
using JoinRpg.Services.Drp.MasterExports;
using JoinRpg.Web.ProjectMasterTools.ClaimRestore;

namespace JoinRpg.WebPortal.Managers.Drp;

/// <summary>
/// Перекладка плана восстановления проекта по выгрузкам мастера (DRP, #5529) в модель страницы.
/// Без обращений к базе — чтобы проверять тестами. Заявки перекладывает <see cref="ClaimRestorePlanBuilder"/>.
/// </summary>
internal static class ProjectRestorePlanViewBuilder
{
    /// <summary>Группы проекта для плана — все, включая удалённые.</summary>
    public static IReadOnlyCollection<ExistingGroupInfo> ExistingGroups(ProjectInfo projectInfo)
        => [.. projectInfo.GroupTree.AllGroups.Select(g => new ExistingGroupInfo(g.Id, g.Name, g.IsActive))];

    /// <summary>Email игроков из выгрузок — чтобы найти пользователей одним запросом.</summary>
    public static IReadOnlyCollection<string> PlayerEmails(IEnumerable<DatedMasterExport> exports)
        => [.. exports
            .SelectMany(e => e.Export.Rows)
            .Select(r => r.PlayerEmail?.Trim())
            .OfType<string>()
            .Where(e => e.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)];

    /// <summary>Id групп из выгрузок, которых в проекте нет: только их и надо проверять на занятость.</summary>
    public static IReadOnlyCollection<CharacterGroupIdentification> UnknownGroupIds(IEnumerable<DatedMasterExport> exports, ProjectInfo projectInfo)
        => [.. exports
            .Where(e => e.Export.Kind == MasterExportKind.Characters)
            .SelectMany(e => e.Export.Rows)
            .SelectMany(r => r.Groups)
            .Select(g => new CharacterGroupIdentification(projectInfo.ProjectId, g.CharacterGroupId))
            .Distinct()
            .Where(id => !projectInfo.GroupTree.Contains(id))];

    /// <summary>Id персонажей из выгрузок, которых в проекте нет.</summary>
    public static IReadOnlyCollection<CharacterIdentification> UnknownCharacterIds(
        IEnumerable<DatedMasterExport> exports, ProjectInfo projectInfo, IReadOnlyCollection<ExistingCharacterInfo> characters)
    {
        var known = characters.Select(c => c.CharacterId).ToHashSet();
        return [.. exports
            .Where(e => e.Export.Kind == MasterExportKind.Characters)
            .SelectMany(e => e.Export.Rows)
            .Select(r => r.CharacterId)
            .OfType<int>()
            .Select(id => new CharacterIdentification(projectInfo.ProjectId, id))
            .Distinct()
            .Where(id => !known.Contains(id))];
    }

    public static ClaimRestorePlanViewModel Build(
        ProjectInfo projectInfo,
        ProjectRestoreSource source,
        ProjectRestorePlan plan,
        IReadOnlyCollection<UserInfoHeader> users,
        DrpOptions options,
        bool canEditRoles)
    {
        var plannedNames = plan.CharacterCreations
            .Where(c => c.Name is not null)
            .ToDictionary(c => c.CharacterId, c => c.Name!);
        var claims = ClaimRestorePlanBuilder.Build(projectInfo, source.Notifications, plan.Claims, users, options, plannedNames);

        var groupNames = new Dictionary<CharacterGroupIdentification, string>();
        foreach (var group in projectInfo.GroupTree.AllGroups)
        {
            groupNames[group.Id] = group.Name;
        }
        foreach (var group in plan.Groups)
        {
            if (group.Name is not null)
            {
                _ = groupNames.TryAdd(group.GroupId, group.Name);
            }
        }
        RestoreGroupOption ToGroup(CharacterGroupIdentification id)
            => new(id, groupNames.TryGetValue(id, out var name) ? name : id.CharacterGroupId.ToString());

        var characters = source.Notifications.Characters.ToDictionary(c => c.CharacterId);
        var existingClaims = source.Notifications.Claims.ToDictionary(c => c.ClaimId);
        var claimCharacterNames = plan.Claims.DistinctBy(c => c.ClaimId).ToDictionary(c => c.ClaimId, c => c.CharacterName);

        CharacterRestoreRowViewModel ToCharacterRow(CharacterRestorePlanRow row)
        {
            var current = characters.GetValueOrDefault(row.CharacterId);
            return new CharacterRestoreRowViewModel(
                row.CharacterId,
                ToActionView(row.Action),
                row.Name,
                current?.Name,
                row.Groups is null ? null : [.. row.Groups.Select(ToGroup)],
                [.. row.CurrentGroups.Select(ToGroup)],
                row.GroupsApplyByDefault,
                [.. row.Fields.Select(f => ToFieldView(projectInfo, f))],
                row.ExportedAt,
                row.ApplyByDefault,
                [.. row.Warnings.Select(ToWarningView)])
            {
                // Домен ответственного нового персонажа не ставит — показываем справкой.
                ExportResponsibleMaster = row.Action == ProjectRestoreAction.Create ? row.ResponsibleMaster : null,
            };
        }

        string? ClaimCharacterName(ClaimIdentification claimId)
        {
            if (claimCharacterNames.GetValueOrDefault(claimId) is { } name)
            {
                return name;
            }
            return existingClaims.TryGetValue(claimId, out var claim) && characters.TryGetValue(claim.CharacterId, out var character)
                ? character.Name
                : null;
        }

        return claims with
        {
            CanEditRoles = canEditRoles,
            Exports = [.. plan.Exports.Select(e => ToExportView(projectInfo, e))],
            // Совпадающие с базой строки без пометок не показываем — менять в них нечего.
            Groups = [.. plan.Groups
                .Where(g => g.Action != ProjectRestoreAction.Nothing || g.Warnings.Count > 0)
                .Select(g => new GroupRestoreRowViewModel(
                    g.GroupId,
                    ToActionView(g.Action),
                    g.Name,
                    g.CurrentName,
                    g.ExportedAt,
                    g.ApplyByDefault,
                    [.. g.Warnings.Select(ToWarningView)]))],
            CharacterCreations = [.. plan.CharacterCreations.Select(ToCharacterRow)],
            CharacterChanges = [.. plan.CharacterChanges
                .Where(c => c.Action != ProjectRestoreAction.Nothing || c.Warnings.Count > 0)
                .Select(ToCharacterRow)],
            ClaimFields = [.. plan.ClaimFields.Select(c => new ClaimFieldsRestoreRowViewModel(
                c.ClaimId,
                ClaimCharacterName(c.ClaimId),
                [.. c.Fields.Select(f => ToFieldView(projectInfo, f))],
                c.ExportedAt,
                c.ApplyByDefault,
                [.. c.Warnings.Select(ToWarningView)]))],
            Notes = [.. plan.Notes.Select(n => new ProjectRestoreNoteView(ToWarningView(n.Warning), n.ExportedAt, n.RowNumber))],
        };
    }

    public static MasterExportUploadViewModel ToUploadView(string fileName, ParsedMasterExport export)
        => new(
            fileName,
            ToKindView(export.Kind),
            export.ProjectId,
            export.SuggestedDate,
            export.Rows.Count,
            [.. export.Warnings.Distinct().Select(ToWarningView)],
            MasterExportPayload.Serialize(export));

    private static MasterExportPlanInfoView ToExportView(ProjectInfo projectInfo, MasterExportPlanInfo info)
        => new(
            ToKindView(info.Kind),
            info.ExportedAt,
            info.Used,
            MappedCount: info.Columns.Count(c => c.Field is not null),
            Columns: [.. info.Columns
                .Where(c => c.Field is null || c.Warnings.Count > 0)
                .Select(c => new MasterExportColumnView(
                    c.Header,
                    c.Field is null ? null : projectInfo.GetFieldById(c.Field).Name,
                    [.. c.Warnings.Select(ToWarningView)]))]);

    private static FieldRestoreChangeView ToFieldView(ProjectInfo projectInfo, FieldRestoreChange change)
    {
        var field = projectInfo.GetFieldById(change.Field);
        return new FieldRestoreChangeView(
            change.Field,
            field.Name,
            change.Value,
            CurrentDisplay: new FieldWithValue(field, change.CurrentValue).DisplayString,
            NewDisplay: new FieldWithValue(field, change.Value).DisplayString,
            change.ExportedAt,
            ToKindView(change.ExportKind),
            change.ApplyByDefault,
            [.. change.Warnings.Select(ToWarningView)],
            [.. change.RestoreWarnings.Select(ToWarningView)]);
    }

    private static ClaimRestoreActionView ToActionView(ProjectRestoreAction action) => action switch
    {
        ProjectRestoreAction.Create => ClaimRestoreActionView.Create,
        ProjectRestoreAction.Change => ClaimRestoreActionView.Change,
        ProjectRestoreAction.Nothing => ClaimRestoreActionView.Nothing,
        _ => throw new ArgumentOutOfRangeException(nameof(action), action, null),
    };

    // Перечисления страницы повторяют доменные по именам; полноту проверяют тесты перебором значений.
    internal static ProjectRestoreWarningView ToWarningView(ProjectRestoreWarning warning)
        => MapByName<ProjectRestoreWarning, ProjectRestoreWarningView>(warning);

    internal static MasterExportFieldWarningView ToWarningView(MasterExportFieldWarning warning)
        => MapByName<MasterExportFieldWarning, MasterExportFieldWarningView>(warning);

    internal static MasterExportWarningView ToWarningView(MasterExportWarning warning)
        => MapByName<MasterExportWarning, MasterExportWarningView>(warning);

    internal static MasterExportUploadErrorView ToErrorView(MasterExportFormatError error)
        => MapByName<MasterExportFormatError, MasterExportUploadErrorView>(error);

    internal static MasterExportKindView ToKindView(MasterExportKind kind)
        => MapByName<MasterExportKind, MasterExportKindView>(kind);

    private static TView MapByName<TDomain, TView>(TDomain value)
        where TDomain : struct, Enum
        where TView : struct, Enum
        => Enum.TryParse<TView>(value.ToString(), ignoreCase: false, out var view) && Enum.IsDefined(view)
            ? view
            : throw new ArgumentOutOfRangeException(nameof(value), value, null);
}
