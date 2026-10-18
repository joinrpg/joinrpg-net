using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>Выгрузка мастера и дата, на которую она верна (подтверждена мастером при загрузке).</summary>
public record DatedMasterExport(ParsedMasterExport Export, DateTimeOffset ExportedAt);

/// <summary>Группа персонажей в восстановленной базе.</summary>
public record ExistingGroupInfo(CharacterGroupIdentification GroupId, string Name, bool IsActive);

/// <summary>Всё, по чему строится план восстановления проекта по выгрузкам мастера и уведомлениям (#5529).</summary>
/// <param name="Project">Метаданные проекта — поля уже заведены мастером заново.</param>
/// <param name="Notifications">
/// Уведомления и состояние заявок и персонажей в восстановленной базе. Для персонажей нужны
/// <see cref="ExistingCharacterInfo.Groups"/>, <see cref="ExistingCharacterInfo.FieldLayer"/>,
/// <see cref="ExistingCharacterInfo.UpdatedAt"/>, для заявок — <see cref="ExistingClaimInfo.FieldLayer"/>,
/// <see cref="ExistingClaimInfo.LastUpdated"/>, <see cref="ExistingClaimInfo.LastChangedNotByRestore"/>.
/// </param>
/// <param name="Exports">Выгрузки в любом порядке — план сам упорядочит их по дате.</param>
/// <param name="Groups">Группы проекта в восстановленной базе, включая удалённые.</param>
/// <param name="UsersByEmail">Пользователи по email игроков из выгрузок (регистр не важен); кого нет — того не нашли.</param>
/// <param name="LostAt">Момент аварии: что в базе менялось позже — правили уже после восстановления.</param>
public record ProjectRestoreSource(
    ProjectInfo Project,
    ClaimRestoreSource Notifications,
    IReadOnlyCollection<DatedMasterExport> Exports,
    IReadOnlyCollection<ExistingGroupInfo> Groups,
    IReadOnlyDictionary<string, UserIdentification> UsersByEmail,
    DateTimeOffset LostAt)
{
    /// <summary>
    /// Id групп из выгрузок, которые в базе заняты группами других проектов: создать группу с прежним id нельзя.
    /// </summary>
    public IReadOnlyCollection<int> GroupIdsTakenByOtherProjects { get; init; } = [];

    /// <summary>Id персонажей из выгрузок, занятые персонажами других проектов.</summary>
    public IReadOnlyCollection<int> CharacterIdsTakenByOtherProjects { get; init; } = [];
}

/// <summary>Что сделать с группой или персонажем.</summary>
public enum ProjectRestoreAction
{
    /// <summary>Нет в базе — создать с прежним id.</summary>
    Create,

    /// <summary>Есть в базе, но отличается от выгрузки — изменить только отличающееся.</summary>
    Change,

    /// <summary>Совпадает с выгрузкой или менять нельзя (см. пометки).</summary>
    Nothing,
}

/// <summary>В чём план восстановления по выгрузкам не уверен — мастер проверяет и решает сам.</summary>
public enum ProjectRestoreWarning
{
    /// <summary>Выгрузка из другого проекта — пропущена целиком.</summary>
    ExportFromOtherProject,

    /// <summary>Строка выгрузки без id персонажа (заявки) — пропущена.</summary>
    RowWithoutId,

    /// <summary>Сущность встречается в выгрузке несколько раз — взята первая строка.</summary>
    DuplicateRow,

    /// <summary>
    /// Ссылка строки (на неё саму или на её группы) ведёт в другой проект
    /// (<see cref="MasterExportWarning.ProjectIdMismatch"/>) — строка пропущена.
    /// </summary>
    RowFromOtherProject,

    /// <summary>
    /// Заявка, которой принадлежат поля, создаётся только по решению мастера (не по умолчанию или нужен выбор
    /// игрока либо персонажа) — поля тоже не по умолчанию.
    /// </summary>
    ClaimNeedsDecision,

    /// <summary>
    /// Выгрузка старше бэкапа: восстановленная база свежее, поэтому по умолчанию не применяется. У существующей
    /// сущности — на поле или наборе групп, у создаваемой — на всей строке.
    /// </summary>
    ExportOlderThanBackup,

    /// <summary>
    /// Значение правили руками после восстановления: сущность менялась после аварии, а непустое меняемое
    /// значение в базе не совпадает ни с одной загруженной выгрузкой (иначе его поставил инструмент).
    /// Ставится на поле (<see cref="FieldRestoreChange.RestoreWarnings"/>) или на набор групп
    /// (<see cref="CharacterRestorePlanRow.GroupsApplyByDefault"/>) — только оно по умолчанию не применяется;
    /// у строки — сводно.
    /// </summary>
    ChangedAfterRestore,

    /// <summary>Группа в базе называется иначе, чем в выгрузке. План группы не переименовывает.</summary>
    GroupNameDiffers,

    /// <summary>Id группы занят группой другого проекта — группу не создать, персонажам она не ставится.</summary>
    GroupIdTakenByOtherProject,

    /// <summary>Группа в базе удалена — персонажам она не ставится.</summary>
    GroupInactive,

    /// <summary>Имя группы в выгрузке не сопоставилось со ссылкой (<see cref="MasterExportWarning.GroupsCountMismatch"/>) — указать вручную.</summary>
    GroupNameUnknown,

    /// <summary>Персонаж входил в группу, которую поставить нельзя (см. пометки группы).</summary>
    GroupSkipped,

    /// <summary>Id персонажа занят персонажем другого проекта — создать нельзя.</summary>
    CharacterIdTakenByOtherProject,

    /// <summary>Персонаж в базе удалён — план его не меняет.</summary>
    CharacterInactive,

    /// <summary>Имя персонажа в базе другое, а поле имени не сопоставлено — план персонажей не переименовывает.</summary>
    CharacterNameDiffers,

    /// <summary>Имени персонажа в выгрузке нет — указать вручную.</summary>
    CharacterNameUnknown,

    /// <summary>
    /// Персонаж есть в более старой выгрузке персонажей, но нет в более новой: удалён — или новая выгрузка
    /// отфильтрована. План берёт его из старой выгрузки; создание — только по решению мастера.
    /// </summary>
    MissingInNewerExport,
}

/// <summary>Пометка, не относящаяся к одной строке плана: про выгрузку целиком или про её строку.</summary>
/// <param name="ExportedAt">Дата выгрузки, о которой речь.</param>
/// <param name="RowNumber">Номер строки в файле выгрузки, если речь о строке.</param>
public record ProjectRestoreNote(ProjectRestoreWarning Warning, DateTimeOffset ExportedAt, int? RowNumber);

/// <summary>Как колонки выгрузки сопоставились с полями проекта — чтобы показать несопоставленные.</summary>
/// <param name="Used">false — выгрузка не участвует в плане (другой проект).</param>
public record MasterExportPlanInfo(
    MasterExportKind Kind,
    DateTimeOffset ExportedAt,
    bool Used,
    IReadOnlyList<MasterExportColumnMapping> Columns);

/// <summary>Значение поля из выгрузки, отличающееся от базы.</summary>
/// <param name="Value">Хранимое значение из выгрузки; null — очистить.</param>
/// <param name="CurrentValue">Значение в базе сейчас — альтернатива.</param>
/// <param name="ExportedAt">
/// Дата выгрузки, из которой значение. Поля берутся только из выгрузок: по каждому полю — самая свежая
/// выгрузка, где колонка поля сопоставлена.
/// </param>
/// <param name="ExportKind">Из выгрузки персонажей или заявок (у одобренной заявки там поля персонажа).</param>
/// <param name="ApplyByDefault">
/// Применять ли, если мастер ничего не выбрал: сопоставление колонки и разбор без пометок, выгрузка не старше
/// бэкапа и значение в базе не правили руками (<see cref="RestoreWarnings"/>).
/// </param>
/// <param name="Warnings">Пометки сопоставления колонки и разбора значения.</param>
/// <param name="RestoreWarnings">
/// Пометки слияния по этому полю: <see cref="ProjectRestoreWarning.ChangedAfterRestore"/>,
/// <see cref="ProjectRestoreWarning.ExportOlderThanBackup"/>.
/// </param>
public record FieldRestoreChange(
    ProjectFieldIdentification Field,
    string? Value,
    string? CurrentValue,
    DateTimeOffset ExportedAt,
    MasterExportKind ExportKind,
    bool ApplyByDefault,
    IReadOnlyList<MasterExportFieldWarning> Warnings,
    IReadOnlyList<ProjectRestoreWarning> RestoreWarnings);

/// <summary>Группа из выгрузки персонажей.</summary>
/// <param name="Name">Имя из самой свежей выгрузки, где группа есть; null — не сопоставилось.</param>
/// <param name="CurrentName">Имя в базе, если группа там есть.</param>
public record GroupRestorePlanRow(
    CharacterGroupIdentification GroupId,
    ProjectRestoreAction Action,
    string? Name,
    string? CurrentName,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyCollection<ProjectRestoreWarning> Warnings);

/// <summary>
/// Персонаж из выгрузки персонажей (или персонаж базы, чьи поля есть в выгрузке заявок у одобренной заявки).
/// Для <see cref="ProjectRestoreAction.Change"/> заполнено только то, что меняется.
/// </summary>
/// <param name="Name">Имя из выгрузки (для создания).</param>
/// <param name="Groups">Обычные группы, в которые поставить персонажа (замена множества); null — не менять.</param>
/// <param name="CurrentGroups">Группы в базе сейчас.</param>
/// <param name="Fields">
/// Отличающиеся от базы значения полей (при создании — все непустые); у каждого поля своя выгрузка-источник.
/// </param>
/// <param name="ExportedAt">Дата выгрузки персонажей, из которой взяты имя и группы.</param>
/// <param name="ResponsibleMaster">Ответственный из выгрузки (для создания), если сопоставился с мастером.</param>
/// <param name="ApplyByDefault">
/// Применять ли строку по умолчанию: есть что применять по умолчанию. Какие именно изменения — решают
/// <see cref="GroupsApplyByDefault"/> и <see cref="FieldRestoreChange.ApplyByDefault"/>.
/// </param>
/// <param name="Warnings">Пометки строки, включая сводные по полям и группам.</param>
public record CharacterRestorePlanRow(
    CharacterIdentification CharacterId,
    ProjectRestoreAction Action,
    string? Name,
    IReadOnlyCollection<CharacterGroupIdentification>? Groups,
    IReadOnlyCollection<CharacterGroupIdentification> CurrentGroups,
    IReadOnlyList<FieldRestoreChange> Fields,
    UserIdentification? ResponsibleMaster,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyCollection<ProjectRestoreWarning> Warnings)
{
    /// <summary>
    /// Применять ли смену групп по умолчанию: false, если набор в базе правили руками
    /// (<see cref="ProjectRestoreWarning.ChangedAfterRestore"/>) или группы из выгрузки старше бэкапа.
    /// </summary>
    public bool GroupsApplyByDefault { get; init; } = true;
}

/// <summary>
/// Поля, которые хранит заявка (её слой), из выгрузок заявок: поля заявки и, если заявка по плану
/// не одобрена, предложения игрока по полям персонажа — из выгрузок новее последней, где она была одобрена.
/// Предложение сравнивается с тем, что показала бы выгрузка (предложение, а без него — значение персонажа
/// по плану); значение, равное персонажу, при текущем предложении — очистка предложения. Потерянная заявка,
/// одобренная по итогу и создающая персонажа сама, получает здесь и поля персонажа. Применяется последним.
/// </summary>
/// <param name="ApplyByDefault">
/// Есть поле, применяемое по умолчанию, и заявка не требует решения мастера (<see cref="ProjectRestoreWarning.ClaimNeedsDecision"/>).
/// </param>
public record ClaimFieldsRestorePlanRow(
    ClaimIdentification ClaimId,
    IReadOnlyList<FieldRestoreChange> Fields,
    DateTimeOffset ExportedAt,
    bool ApplyByDefault,
    IReadOnlyCollection<ProjectRestoreWarning> Warnings);

/// <summary>
/// План восстановления проекта. Порядок применения — порядок свойств:
/// группы → создание персонажей (с полями и группами) → заявки (сначала освобождающие персонажей) →
/// изменение полей и групп существующих персонажей → поля заявок.
/// </summary>
/// <remarks>
/// Изменения существующих персонажей — после заявок: одобрение заявки переносит слой заявки (предложения
/// игрока) в персонажа и затёрло бы только что восстановленные из выгрузки значения.
/// </remarks>
/// <param name="Exports">Сопоставление колонок каждой выгрузки — по дате, от старой к новой.</param>
/// <param name="Groups">Все группы из выгрузок, включая совпадающие с базой (<see cref="ProjectRestoreAction.Nothing"/>).</param>
/// <param name="CharacterCreations">Персонажи из выгрузок персонажей, которых нет в базе (создать или нельзя создать).</param>
/// <param name="Claims">Заявки — как у <see cref="ClaimRestorePlanner"/>, плюс заявки из выгрузки заявок.</param>
/// <param name="CharacterChanges">
/// Персонажи базы из выгрузок персонажей и персонажи базы с полями из выгрузок заявок, включая совпадающие с базой.
/// </param>
/// <param name="ClaimFields">Только заявки, у которых поля отличаются от базы.</param>
/// <param name="Notes">Пометки про выгрузки целиком и пропущенные строки.</param>
public record ProjectRestorePlan(
    IReadOnlyList<MasterExportPlanInfo> Exports,
    IReadOnlyList<GroupRestorePlanRow> Groups,
    IReadOnlyList<CharacterRestorePlanRow> CharacterCreations,
    IReadOnlyList<ClaimRestorePlanRow> Claims,
    IReadOnlyList<CharacterRestorePlanRow> CharacterChanges,
    IReadOnlyList<ClaimFieldsRestorePlanRow> ClaimFields,
    IReadOnlyList<ProjectRestoreNote> Notes)
{
    /// <summary>Все строки персонажей по id — для показа; применять — в порядке свойств плана.</summary>
    public IReadOnlyList<CharacterRestorePlanRow> Characters => [.. CharacterCreations.Concat(CharacterChanges).OrderBy(c => c.CharacterId.CharacterId)];
}
