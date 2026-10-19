using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.MasterExports;

/// <summary>Что выгружено: список персонажей или список заявок.</summary>
public enum MasterExportKind
{
    Characters,
    Claims,
}

/// <summary>Группа, в которую входит персонаж: id из ссылки «Входит в группы», имя — из колонки «Группы».</summary>
/// <param name="Name">Пустая строка, если имена не удалось сопоставить со ссылками (см. <see cref="MasterExportWarning.GroupsCountMismatch"/>).</param>
public record MasterExportGroupRef(int CharacterGroupId, string Name);

/// <summary>Ячейка колонки, не распознанной как системная, — кандидат в значение поля проекта.</summary>
/// <param name="Header">Заголовок колонки — у полей проекта это имя поля.</param>
/// <param name="Value">Значение; пустая строка — поле не заполнено.</param>
/// <param name="Truncated">
/// Excel-выгрузка обрезала слишком длинное значение. Пометка «(обрезано) … ...» из значения уже убрана —
/// в <paramref name="Value"/> лежит сохранившееся начало текста.
/// </param>
public record MasterExportCell(string Header, string Value, bool Truncated);

/// <summary>Тип персонажа из колонки «Занят?» выгрузки персонажей.</summary>
/// <param name="IsHot">
/// Горячий ли. У «Занят» и «Обсуждается» выгрузка этого не показывает — там false.
/// </param>
public record MasterExportCharacterType(CharacterType Type, bool IsHot)
{
    public static MasterExportCharacterType Player { get; } = new(CharacterType.Player, false);
}

/// <summary>Строка выгрузки.</summary>
/// <param name="RowNumber">Номер строки в файле, считая строку заголовков первой, — чтобы мастер нашёл её у себя.</param>
/// <param name="CharacterId">Из ссылки на персонажа; есть только в выгрузке персонажей.</param>
/// <param name="ClaimId">Из ссылки на заявку; есть только в выгрузке заявок.</param>
/// <param name="Name">Имя персонажа.</param>
/// <param name="FeePaid">«Уплачено» — сумма одобренных оплат на момент выгрузки; есть только в выгрузке заявок.</param>
/// <param name="OtherCells">Все колонки, не распознанные как системные, — кандидаты в поля проекта. Пустые тоже.</param>
public record MasterExportRow(
    int RowNumber,
    int? CharacterId,
    int? ClaimId,
    string? Name,
    IReadOnlyList<MasterExportGroupRef> Groups,
    string? ResponsibleMasterName,
    string? PlayerEmail,
    string? PlayerDisplayName,
    ClaimStatus? Status,
    int? TotalFee,
    int? FeePaid,
    DateTimeOffset? ClaimCreatedAt,
    DateTimeOffset? ClaimUpdatedAt,
    IReadOnlyList<MasterExportCell> OtherCells)
{
    /// <summary>Что не так именно с этой строкой. Те же значения попадают и в предупреждения всей выгрузки.</summary>
    public IReadOnlyList<MasterExportWarning> Warnings { get; init; } = [];

    /// <summary>
    /// Тип персонажа из «Занят?»; есть только в выгрузке персонажей. null — неизвестен: колонки нет, она
    /// пустая, в ней «Неизвестно» или нераспознанное значение (<see cref="MasterExportWarning.UnknownBusyStatus"/>).
    /// </summary>
    public MasterExportCharacterType? CharacterType { get; init; }
}

/// <summary>Что в выгрузке разобралось не полностью. Разбор при этом продолжается.</summary>
public enum MasterExportWarning
{
    /// <summary>
    /// Ссылки ведут в разные проекты; <see cref="ParsedMasterExport.ProjectId"/> — самый частый из них.
    /// У строки — ссылка на неё саму или на её группы ведёт не в <see cref="ParsedMasterExport.ProjectId"/>.
    /// </summary>
    ProjectIdMismatch,

    /// <summary>Число имён в «Группы» не совпало с числом ссылок в «Входит в группы» (например, « | » в имени группы).</summary>
    GroupsCountMismatch,

    /// <summary>
    /// В непустой строке в колонке ссылок не ссылка на персонажа (заявку) — id у строки не будет.
    /// </summary>
    RowWithoutLink,

    /// <summary>Один и тот же персонаж (заявка) встречается в нескольких строках.</summary>
    DuplicateEntityId,

    /// <summary>Статус заявки не совпал ни с одним известным названием.</summary>
    UnknownClaimStatus,

    /// <summary>Число или дата в системной колонке не разобрались — значение пропущено.</summary>
    InvalidValue,

    /// <summary>Есть обрезанные Excel-выгрузкой значения (<see cref="MasterExportCell.Truncated"/>).</summary>
    TruncatedValues,

    /// <summary>Несколько колонок-кандидатов в поля с одинаковым заголовком — по имени их не различить.</summary>
    DuplicateHeaders,

    /// <summary>
    /// Значение «Занят?» в выгрузке персонажей не совпало ни с одним известным — тип персонажа неизвестен.
    /// В конце: в строке выгрузки для острова значения хранятся числами.
    /// </summary>
    UnknownBusyStatus,
}

/// <summary>Разобранная выгрузка мастера.</summary>
/// <param name="ProjectId">Проект из ссылок на персонажей или заявки.</param>
/// <param name="SuggestedDate">
/// На какой момент выгрузка — из свойств книги: создана (это момент выгрузки), иначе изменена.
/// </param>
/// <param name="OtherHeaders">Заголовки колонок-кандидатов в поля проекта, в порядке файла.</param>
public record ParsedMasterExport(
    MasterExportKind Kind,
    int ProjectId,
    DateTimeOffset? SuggestedDate,
    IReadOnlyList<string> OtherHeaders,
    IReadOnlyList<MasterExportRow> Rows,
    IReadOnlyList<MasterExportWarning> Warnings);

/// <summary>Почему файл не удалось разобрать как выгрузку.</summary>
public enum MasterExportFormatError
{
    /// <summary>Не xlsx (например, CSV или старый .xls) — разбираем только Excel-выгрузку.</summary>
    UnsupportedFile,

    /// <summary>Нет строки заголовков.</summary>
    Empty,

    /// <summary>Есть заголовки, но нет ни одной строки данных.</summary>
    NoRows,

    /// <summary>Не нашлось колонки со ссылками на персонажей или заявки.</summary>
    LinkColumnNotFound,

    /// <summary>Подошли и сигнатура выгрузки персонажей, и сигнатура выгрузки заявок.</summary>
    AmbiguousKind,

    /// <summary>
    /// Ссылки есть, но это не выгрузка списка персонажей или заявок (например, финансовая выгрузка
    /// или «Мои заявки»): нет колонок, по которым эти выгрузки узнаются.
    /// </summary>
    UnknownExportType,
}

/// <summary>Файл не похож на выгрузку JoinRPG — разбирать нечего.</summary>
public class MasterExportFormatException(MasterExportFormatError error, string fileName, Exception? inner = null)
    : Exception($"Master export {fileName} is not recognized: {error}", inner)
{
    public MasterExportFormatError Error { get; } = error;
}
