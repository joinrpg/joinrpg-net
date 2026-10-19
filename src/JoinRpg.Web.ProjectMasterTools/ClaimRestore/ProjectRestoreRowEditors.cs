using System.Globalization;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>Загруженная выгрузка мастера: остров держит её у себя, мастер подтверждает дату (DRP, #5529).</summary>
public class UploadedExport(MasterExportUploadViewModel upload)
{
    private const string LocalInputFormat = "yyyy-MM-ddTHH:mm";

    public MasterExportUploadViewModel Upload { get; } = upload;

    /// <summary>На какой момент выгрузка. По умолчанию — дата из свойств книги; если её там нет, вводит мастер.</summary>
    public DateTimeOffset? ExportedAt { get; set; } = upload.SuggestedDate;

    /// <summary>Значение для <c>input type="datetime-local"</c> — в часовом поясе браузера.</summary>
    public string LocalInputValue => ExportedAt?.ToLocalTime().ToString(LocalInputFormat, CultureInfo.InvariantCulture) ?? "";

    /// <summary>Принимает значение <c>input type="datetime-local"</c>; пусто или не разобралось — даты нет.</summary>
    public void SetFromLocalInput(string? value)
    {
        if (DateTime.TryParseExact(value, LocalInputFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var local)
            || DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out local))
        {
            local = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            ExportedAt = new DateTimeOffset(local, TimeZoneInfo.Local.GetUtcOffset(local));
        }
        else
        {
            ExportedAt = null;
        }
    }

    /// <summary>Выгрузка для плана; null — даты нет, без неё не понять, что свежее.</summary>
    public MasterExportForPlan? ToPlanExport() => ExportedAt is { } at ? new MasterExportForPlan(Upload.Payload, at) : null;
}

/// <summary>Значение поля с отметкой «записать».</summary>
public class FieldRestoreSelection(FieldRestoreChangeView change)
{
    public FieldRestoreChangeView Change { get; } = change;

    public bool Selected { get; set; } = change.ApplyByDefault;
}

/// <summary>Общее у строк плана по выгрузкам: отметка и итог применения.</summary>
public abstract class RestoreRowEditorBase
{
    public bool Selected { get; set; }

    /// <summary>Чем закончилось применение: null — не применяли.</summary>
    public bool? Succeeded { get; set; }

    public string? Error { get; set; }

    public abstract bool CanApply { get; }

    /// <summary>Что мешает применить строку; null — можно применять.</summary>
    public abstract string? BlockingProblem { get; }

    protected void CopyResultFrom(RestoreRowEditorBase previous)
    {
        Succeeded = previous.Succeeded;
        Error = previous.Error;
    }

    protected static IReadOnlyList<RestoreFieldValueRequest> SelectedFields(IEnumerable<FieldRestoreSelection> fields)
        => [.. fields.Where(f => f.Selected).Select(f => new RestoreFieldValueRequest(f.Change.FieldId, f.Change.Value))];

    protected static void CopySelections(IReadOnlyList<FieldRestoreSelection> target, IReadOnlyList<FieldRestoreSelection> previous)
    {
        var old = previous.ToDictionary(f => f.Change.FieldId);
        foreach (var field in target)
        {
            if (old.TryGetValue(field.Change.FieldId, out var was) && was.Change.Value == field.Change.Value)
            {
                field.Selected = was.Selected;
            }
        }
    }
}

/// <summary>Группа из выгрузки: создать с прежним id.</summary>
public sealed class GroupRestoreRowEditor : RestoreRowEditorBase
{
    private readonly bool canEditRoles;

    public GroupRestoreRowEditor(GroupRestoreRowViewModel row, bool canEditRoles)
    {
        Row = row;
        this.canEditRoles = canEditRoles;
        Name = row.Name ?? "";
        Selected = CanApply && row.ApplyByDefault && BlockingProblem is null;
    }

    public GroupRestoreRowViewModel Row { get; }

    public string Name { get; set; }

    public override bool CanApply => canEditRoles && Row.Action == ClaimRestoreActionView.Create;

    public override string? BlockingProblem => string.IsNullOrWhiteSpace(Name) ? "Укажите имя группы" : null;

    public GroupRestoreApplyRequest ToApplyRequest() => new(Row.GroupId, Name.Trim());

    public void CopyEditsFrom(GroupRestoreRowEditor previous)
    {
        CopyResultFrom(previous);
        if (previous.Succeeded == true || previous.Row.Action != Row.Action)
        {
            return;
        }
        Selected = previous.Selected;
        Name = previous.Name;
    }
}

/// <summary>Персонаж из выгрузки: создать с прежним id или изменить группы и поля.</summary>
public sealed class CharacterRestoreRowEditor : RestoreRowEditorBase
{
    private readonly bool canEditRoles;

    public CharacterRestoreRowEditor(CharacterRestoreRowViewModel row, bool canEditRoles)
    {
        Row = row;
        this.canEditRoles = canEditRoles;
        Name = row.Name ?? "";
        ApplyGroups = row.Groups is not null && row.GroupsApplyByDefault;
        Fields = [.. row.Fields.Select(f => new FieldRestoreSelection(f))];
        Selected = CanApply && row.ApplyByDefault && BlockingProblem is null;
    }

    public CharacterRestoreRowViewModel Row { get; }

    public bool Create => Row.Action == ClaimRestoreActionView.Create;

    /// <summary>Имя нового персонажа.</summary>
    public string Name { get; set; }

    /// <summary>Ставить ли группы из выгрузки.</summary>
    public bool ApplyGroups { get; set; }

    public IReadOnlyList<FieldRestoreSelection> Fields { get; }

    public override bool CanApply => canEditRoles && Row.Action != ClaimRestoreActionView.Nothing;

    public override string? BlockingProblem
    {
        get
        {
            if (Create)
            {
                return string.IsNullOrWhiteSpace(Name) ? "Укажите имя персонажа" : null;
            }
            return ApplyGroups || Fields.Any(f => f.Selected) ? null : "Не отмечено ни одного изменения";
        }
    }

    /// <summary>Группы, которые получит персонаж; null — не менять.</summary>
    public IReadOnlyList<CharacterGroupIdentification>? GroupsToApply
        => ApplyGroups && Row.Groups is { } groups ? [.. groups.Select(g => g.GroupId)] : null;

    public CharacterRestoreApplyRequest ToApplyRequest()
        => new(Row.CharacterId, Create, Create ? Name.Trim() : null, GroupsToApply, SelectedFields(Fields))
        {
            CharacterType = Row.CharacterType,
            IsHot = Row.IsHot,
        };

    public void CopyEditsFrom(CharacterRestoreRowEditor previous)
    {
        CopyResultFrom(previous);
        if (previous.Succeeded == true || previous.Row.Action != Row.Action)
        {
            return;
        }
        Selected = previous.Selected;
        Name = previous.Name;
        ApplyGroups = previous.ApplyGroups && Row.Groups is not null;
        CopySelections(Fields, previous.Fields);
    }
}

/// <summary>Поля заявки из выгрузки заявок.</summary>
public sealed class ClaimFieldsRestoreRowEditor : RestoreRowEditorBase
{
    public ClaimFieldsRestoreRowEditor(ClaimFieldsRestoreRowViewModel row)
    {
        Row = row;
        Fields = [.. row.Fields.Select(f => new FieldRestoreSelection(f))];
        Selected = row.ApplyByDefault && BlockingProblem is null;
    }

    public ClaimFieldsRestoreRowViewModel Row { get; }

    public IReadOnlyList<FieldRestoreSelection> Fields { get; }

    public override bool CanApply => true;

    public override string? BlockingProblem => Fields.Any(f => f.Selected) ? null : "Не отмечено ни одного поля";

    public ClaimFieldsRestoreApplyRequest ToApplyRequest() => new(Row.ClaimId, SelectedFields(Fields));

    public void CopyEditsFrom(ClaimFieldsRestoreRowEditor previous)
    {
        CopyResultFrom(previous);
        if (previous.Succeeded == true)
        {
            return;
        }
        Selected = previous.Selected;
        CopySelections(Fields, previous.Fields);
    }
}

/// <summary>
/// Зависимости строк плана: группы и персонажи, которых план хочет создать, ещё не созданы, пока их строки
/// не применились. Строка, которая на них ссылается, не применяется — с понятной причиной.
/// </summary>
public class RestoreDependencies
{
    private readonly HashSet<CharacterGroupIdentification> pendingGroups;
    private readonly HashSet<CharacterIdentification> pendingCharacters;
    private readonly HashSet<ClaimIdentification> pendingClaims;

    public RestoreDependencies(ClaimRestorePlanViewModel plan)
    {
        pendingGroups = [.. plan.Groups.Where(g => g.Action == ClaimRestoreActionView.Create).Select(g => g.GroupId)];
        pendingCharacters = [.. plan.CharacterCreations.Where(c => c.Action == ClaimRestoreActionView.Create).Select(c => c.CharacterId)];
        // Поля заявки ждут и «изменить»: план полей считает, что заявка уже в плановом статусе. Если заявку
        // по плану одобряют, а одобрение не применилось, её предложения игрока затёрли бы персонажа.
        pendingClaims = [.. plan.Rows.Where(r => r.Action != ClaimRestoreActionView.Nothing).Select(r => r.ClaimId)];
    }

    public void Created(CharacterGroupIdentification groupId) => pendingGroups.Remove(groupId);

    public void Created(CharacterIdentification characterId) => pendingCharacters.Remove(characterId);

    /// <summary>Строка заявки применилась так, как в плане (в том числе статус).</summary>
    public void Created(ClaimIdentification claimId) => pendingClaims.Remove(claimId);

    public string? GroupsProblem(IEnumerable<CharacterGroupIdentification>? groups)
    {
        var missing = groups?.Where(pendingGroups.Contains).Select(g => g.CharacterGroupId).ToList() ?? [];
        return missing.Count == 0
            ? null
            : $"Не создана группа {string.Join(", ", missing)} — сначала примените её строку в разделе «Группы»";
    }

    public string? CharacterProblem(CharacterIdentification? characterId)
        => characterId is not null && pendingCharacters.Contains(characterId)
            ? $"Не создан персонаж {characterId.CharacterId} — сначала примените его строку в разделе «Новые персонажи»"
            : null;

    public string? ClaimProblem(ClaimIdentification claimId)
        => pendingClaims.Contains(claimId)
            ? $"Строка заявки {claimId.ClaimId} не применена так, как в плане (не отмечена, не удалась или выбран другой статус) — поля не пишем, чтобы не затереть персонажа. Сначала примените заявку в разделе «Заявки»"
            : null;
}
