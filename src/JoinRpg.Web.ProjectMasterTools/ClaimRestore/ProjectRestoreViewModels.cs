using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>Что выгружено (DRP, #5529).</summary>
public enum MasterExportKindView
{
    [Display(Name = "Персонажи")]
    Characters,

    [Display(Name = "Заявки")]
    Claims,
}

/// <summary>Почему файл не принят как выгрузка.</summary>
public enum MasterExportUploadErrorView
{
    [Display(Name = "Файл не похож на выгрузку: нужен .xlsx (старый .xls и .csv не подходят)")]
    UnsupportedFile,

    [Display(Name = "Файл пустой — нет даже строки заголовков")]
    Empty,

    [Display(Name = "В выгрузке нет ни одной строки данных")]
    NoRows,

    [Display(Name = "Не нашлось колонки со ссылками на персонажей или заявки")]
    LinkColumnNotFound,

    [Display(Name = "Не понять, выгрузка это персонажей или заявок")]
    AmbiguousKind,

    [Display(Name = "Это не выгрузка списка персонажей или заявок (например, финансовая)")]
    UnknownExportType,

    [Display(Name = "Файл слишком большой")]
    TooLarge,
}

/// <summary>Что в выгрузке разобралось не полностью.</summary>
public enum MasterExportWarningView
{
    [Display(Name = "Ссылки ведут в разные проекты — строки чужого проекта пропущены")]
    ProjectIdMismatch,

    [Display(Name = "Число имён групп не совпало с числом ссылок на группы — у части групп нет имени")]
    GroupsCountMismatch,

    [Display(Name = "В некоторых строках вместо ссылки на персонажа (заявку) что-то другое — такие строки пропущены")]
    RowWithoutLink,

    [Display(Name = "Один и тот же персонаж (заявка) встречается в нескольких строках")]
    DuplicateEntityId,

    [Display(Name = "Статус некоторых заявок не распознан")]
    UnknownClaimStatus,

    [Display(Name = "Некоторые числа или даты в служебных колонках не разобрались")]
    InvalidValue,

    [Display(Name = "Excel обрезал слишком длинные значения — они неполные")]
    TruncatedValues,

    [Display(Name = "Несколько колонок с одинаковым заголовком — по имени их не различить")]
    DuplicateHeaders,
}

/// <summary>В чём сопоставление колонки или значения поля не уверено.</summary>
public enum MasterExportFieldWarningView
{
    [Display(Name = "Колонка не похожа ни на одно поле проекта")]
    ColumnNotMapped,

    [Display(Name = "Под заголовок подходит несколько полей проекта")]
    ColumnAmbiguous,

    [Display(Name = "Заголовок совпал с именем поля только без учёта регистра")]
    ColumnCaseMismatch,

    [Display(Name = "Такой заголовок встречается в выгрузке несколько раз")]
    ColumnDuplicated,

    [Display(Name = "Поле выключено в проекте")]
    FieldInactive,

    [Display(Name = "Поле не может иметь значения (например, заголовок раздела)")]
    FieldCannotHaveValue,

    [Display(Name = "Значение обрезано при выгрузке — оно неполное")]
    Truncated,

    [Display(Name = "Часть текста не совпала ни с одним вариантом — пропущена")]
    VariantNotFound,

    [Display(Name = "Текст можно разложить на варианты по-разному")]
    VariantAmbiguous,

    [Display(Name = "Выбран выключенный вариант")]
    VariantInactive,

    [Display(Name = "У списка с одним выбором распознано несколько вариантов")]
    MultipleVariantsForSingleChoice,

    [Display(Name = "В поле-ссылке на пользователя не идентификаторы или несколько пользователей")]
    InvalidUserIds,

    [Display(Name = "Значение галочки не распознано")]
    InvalidCheckboxValue,
}

/// <summary>Пометки плана восстановления по выгрузкам.</summary>
public enum ProjectRestoreWarningView
{
    [Display(Name = "Выгрузка из другого проекта — пропущена целиком")]
    ExportFromOtherProject,

    [Display(Name = "Строка без ссылки на персонажа (заявку) — пропущена")]
    RowWithoutId,

    [Display(Name = "Повтор: персонаж (заявка) уже встречался в выгрузке выше — строка пропущена")]
    DuplicateRow,

    [Display(Name = "Ссылки строки ведут в другой проект — строка пропущена")]
    RowFromOtherProject,

    [Display(Name = "Заявка создаётся только по вашему решению — её поля тоже")]
    ClaimNeedsDecision,

    [Display(Name = "Выгрузка старше резервной копии — база свежее, по умолчанию не применяется")]
    ExportOlderThanBackup,

    [Display(Name = "Похоже, правили руками после восстановления — по умолчанию не трогаем")]
    ChangedAfterRestore,

    [Display(Name = "В базе группа называется иначе — переименовывать не будем")]
    GroupNameDiffers,

    [Display(Name = "Номер группы занят группой другого проекта — создать нельзя")]
    GroupIdTakenByOtherProject,

    [Display(Name = "Группа удалена — персонажам её не ставим")]
    GroupInactive,

    [Display(Name = "Имя группы в выгрузке не определилось — укажите вручную")]
    GroupNameUnknown,

    [Display(Name = "Часть групп персонажа поставить нельзя (см. раздел «Группы»)")]
    GroupSkipped,

    [Display(Name = "Номер персонажа занят персонажем другого проекта — создать нельзя")]
    CharacterIdTakenByOtherProject,

    [Display(Name = "Персонаж удалён — не меняем")]
    CharacterInactive,

    [Display(Name = "В базе у персонажа другое имя — переименовывать не будем")]
    CharacterNameDiffers,

    [Display(Name = "Имени персонажа в выгрузке нет — укажите вручную")]
    CharacterNameUnknown,

    [Display(Name = "Персонажа нет в более новой выгрузке — удалён или выгрузка отфильтрована")]
    MissingInNewerExport,
}

/// <summary>Откуда взято значение.</summary>
public enum RestoreSourceKindView
{
    [Display(Name = "база")]
    Database,

    [Display(Name = "выгрузка")]
    Export,

    [Display(Name = "уведомление")]
    Notification,
}

/// <summary>Меняемый атрибут заявки.</summary>
public enum ClaimRestoreAttributeKindView
{
    Status,
    ResponsibleMaster,
    Character,
    ManualFee,
}

/// <summary>Значение атрибута из одного источника.</summary>
public record RestoreSourcedValueView<T>(T Value, RestoreSourceKindView Source, DateTimeOffset At);

/// <summary>Спорный атрибут: выбранное значение и отличающиеся значения других источников.</summary>
public record RestoreAttributeView<T>(RestoreSourcedValueView<T> Chosen, IReadOnlyList<RestoreSourcedValueView<T>> Alternatives)
{
    /// <summary>Источники расходятся.</summary>
    public bool HasAlternatives => Alternatives.Count > 0;
}

/// <summary>Источники спорных атрибутов заявки.</summary>
public record ClaimRestoreAttributesView(
    RestoreAttributeView<ClaimStatusView>? Status,
    RestoreAttributeView<UserIdentification>? ResponsibleMaster,
    RestoreAttributeView<string>? CharacterName);

/// <summary>
/// Разобранная выгрузка. Сама выгрузка лежит в <paramref name="Payload"/> — непрозрачной строке, которую
/// остров только хранит и отправляет обратно при построении плана: на сервере выгрузки не хранятся.
/// </summary>
/// <param name="SuggestedDate">Дата из свойств книги; null — в свойствах её нет, дату вводит мастер.</param>
public record MasterExportUploadViewModel(
    string FileName,
    MasterExportKindView Kind,
    int ProjectId,
    DateTimeOffset? SuggestedDate,
    int RowCount,
    IReadOnlyList<MasterExportWarningView> Warnings,
    string Payload);

/// <summary>Итог загрузки файла: либо выгрузка, либо почему она не принята.</summary>
public record MasterExportUploadResult(MasterExportUploadViewModel? Export, MasterExportUploadErrorView? Error);

/// <summary>Выгрузка с подтверждённой мастером датой — для построения плана.</summary>
public record MasterExportForPlan(string Payload, DateTimeOffset ExportedAt);

/// <summary>Построить план восстановления: по уведомлениям и загруженным выгрузкам.</summary>
public record ClaimRestorePlanRequest(ProjectIdentification ProjectId, IReadOnlyList<MasterExportForPlan> Exports);

/// <summary>Колонка выгрузки и поле проекта, если сопоставилась.</summary>
public record MasterExportColumnView(string Header, string? FieldName, IReadOnlyList<MasterExportFieldWarningView> Warnings);

/// <summary>Как колонки выгрузки сопоставились с полями проекта.</summary>
/// <param name="Used">false — выгрузка в плане не участвует (другой проект).</param>
/// <param name="Columns">Только несопоставленные колонки и колонки с пометками.</param>
public record MasterExportPlanInfoView(
    MasterExportKindView Kind,
    DateTimeOffset ExportedAt,
    bool Used,
    int MappedCount,
    IReadOnlyList<MasterExportColumnView> Columns);

/// <summary>Пометка про выгрузку целиком или про её строку.</summary>
public record ProjectRestoreNoteView(ProjectRestoreWarningView Warning, DateTimeOffset ExportedAt, int? RowNumber);

public record RestoreGroupOption(CharacterGroupIdentification GroupId, string Name);

/// <summary>Значение поля из выгрузки, отличающееся от базы.</summary>
/// <param name="Value">Хранимое значение, которое запишется (непрозрачно для острова); null — очистить.</param>
/// <param name="CurrentDisplay">Как поле выглядит сейчас.</param>
/// <param name="NewDisplay">Как оно будет выглядеть.</param>
public record FieldRestoreChangeView(
    ProjectFieldIdentification FieldId,
    string FieldName,
    string? Value,
    string CurrentDisplay,
    string NewDisplay,
    DateTimeOffset ExportedAt,
    MasterExportKindView ExportKind,
    bool ApplyByDefault,
    IReadOnlyList<MasterExportFieldWarningView> Warnings,
    IReadOnlyList<ProjectRestoreWarningView> RestoreWarnings);

/// <summary>Группа из выгрузки персонажей.</summary>
/// <param name="Name">Имя из выгрузки; null — не определилось.</param>
/// <param name="CurrentName">Имя в базе, если группа там есть.</param>
public record GroupRestoreRowViewModel(
    CharacterGroupIdentification GroupId,
    ClaimRestoreActionView Action,
    string? Name,
    string? CurrentName,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyList<ProjectRestoreWarningView> Warnings);

/// <summary>Персонаж из выгрузки: создать или изменить. Для «изменить» заполнено только то, что меняется.</summary>
/// <param name="Groups">Обычные группы после применения; null — не менять.</param>
public record CharacterRestoreRowViewModel(
    CharacterIdentification CharacterId,
    ClaimRestoreActionView Action,
    string? Name,
    string? CurrentName,
    IReadOnlyList<RestoreGroupOption>? Groups,
    IReadOnlyList<RestoreGroupOption> CurrentGroups,
    bool GroupsApplyByDefault,
    IReadOnlyList<FieldRestoreChangeView> Fields,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyList<ProjectRestoreWarningView> Warnings)
{
    /// <summary>
    /// Ответственный из выгрузки (для создания) — только справка: при восстановлении он не ставится,
    /// мастер назначает его после.
    /// </summary>
    public UserIdentification? ExportResponsibleMaster { get; init; }
}

/// <summary>Поля заявки из выгрузки заявок.</summary>
/// <param name="ExistsInDatabase">Заявка есть в базе (иначе создаётся строкой раздела «Заявки» или не создаётся вовсе).</param>
public record ClaimFieldsRestoreRowViewModel(
    ClaimIdentification ClaimId,
    string? CharacterName,
    IReadOnlyList<FieldRestoreChangeView> Fields,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyList<ProjectRestoreWarningView> Warnings,
    bool ExistsInDatabase);

/// <summary>Восстановить потерянную группу с прежним id.</summary>
public record GroupRestoreApplyRequest(CharacterGroupIdentification GroupId, string Name);

/// <summary>Значение поля, выбранное мастером к записи.</summary>
public record RestoreFieldValueRequest(ProjectFieldIdentification FieldId, string? Value);

/// <summary>Создать персонажа с прежним id или изменить существующего.</summary>
/// <param name="Create">Создать (персонажа нет в базе) — тогда нужно <paramref name="Name"/>.</param>
/// <param name="Groups">Обычные группы персонажа — итоговый набор; null — не менять.</param>
/// <param name="Fields">Поля, которые записать (дельта).</param>
public record CharacterRestoreApplyRequest(
    CharacterIdentification CharacterId,
    bool Create,
    string? Name,
    IReadOnlyList<CharacterGroupIdentification>? Groups,
    IReadOnlyList<RestoreFieldValueRequest> Fields);

/// <summary>Записать в заявку поля из выгрузки (дельта).</summary>
public record ClaimFieldsRestoreApplyRequest(ClaimIdentification ClaimId, IReadOnlyList<RestoreFieldValueRequest> Fields);
