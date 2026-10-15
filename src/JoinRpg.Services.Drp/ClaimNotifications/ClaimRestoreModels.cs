using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;

namespace JoinRpg.Services.Drp.ClaimNotifications;

/// <summary>Заявка в восстановленной базе.</summary>
public record ExistingClaimInfo(
    ClaimIdentification ClaimId,
    CharacterIdentification CharacterId,
    UserIdentification Player,
    ClaimStatus Status,
    UserIdentification ResponsibleMaster,
    int? ManualFee)
{
    /// <summary>
    /// Слой заявки (её собственные хранимые значения, <c>Claim.JsonData</c>): поля заявки и, пока заявка
    /// не одобрена, предложения игрока по полям персонажа. Значения персонажа сюда не подмешивать.
    /// null — значения не переданы, то же, что пустой слой. Нужно только при восстановлении по выгрузке (#5529).
    /// </summary>
    public FieldLayerContainer? FieldLayer { get; init; }

    /// <summary>
    /// Когда заявка последний раз менялась. Позже аварии — возможно, её правили руками после восстановления
    /// (<see cref="ClaimRestoreWarning.ChangedAfterRestore"/>).
    /// </summary>
    public DateTimeOffset LastUpdated { get; init; } = DateTimeOffset.MinValue;

    /// <summary>
    /// Сумма одобренных оплат заявки в базе — для сверки с «Уплачено» из выгрузки
    /// (<see cref="ClaimRestoreWarning.FeePaidMismatch"/>).
    /// </summary>
    public int FeePaid { get; init; }
}

/// <summary>Персонаж в восстановленной базе.</summary>
/// <param name="IsSlot">
/// Слот: принятой заявка на нём не бывает — одобрение создаёт из слота нового персонажа, по умолчанию
/// с тем же именем.
/// </param>
/// <param name="DefaultResponsibleMaster">Ответственный по правилам проекта для новой заявки на него.</param>
public record ExistingCharacterInfo(
    CharacterIdentification CharacterId,
    string Name,
    bool IsActive,
    bool IsSlot,
    UserIdentification DefaultResponsibleMaster)
{
    /// <summary>
    /// Обычные группы, в которые персонаж входит напрямую: без корневой и без спецгрупп вариантов полей —
    /// как в колонке «Группы» выгрузки. Нужно только при восстановлении по выгрузке (#5529).
    /// </summary>
    public IReadOnlyCollection<CharacterGroupIdentification> Groups { get; init; } = [];

    /// <summary>
    /// Слой персонажа — его собственные хранимые значения (<c>Character.JsonData</c>, значения — как их пишет
    /// домен: списки — id вариантов через запятую, чекбокс — «on»). Только поля, привязанные к персонажу;
    /// ни поля заявки, ни предложения игрока из неодобренных заявок сюда не класть. Поля без значения можно
    /// не передавать; null — значения не переданы, то же, что пустой слой.
    /// </summary>
    public FieldLayerContainer? FieldLayer { get; init; }

    /// <summary>Когда персонаж последний раз менялся.</summary>
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.MinValue;
}

/// <summary>Мастер проекта — для сопоставления ответственного по имени из уведомлений.</summary>
public record ProjectMasterName(UserIdentification UserId, string DisplayName);

/// <summary>Всё, по чему строится план восстановления заявок одного проекта.</summary>
/// <param name="Events">События по заявкам проекта до момента аварии (<see cref="ClaimNotificationParser"/>).</param>
/// <param name="DefaultResponsibleMaster">Ответственный по правилам проекта для нового персонажа в корневой группе.</param>
/// <param name="BackupTakenAt">Момент бэкапа: события до него в восстановленной базе уже учтены.</param>
public record ClaimRestoreSource(
    IReadOnlyCollection<ClaimNotificationEvent> Events,
    IReadOnlyCollection<ExistingClaimInfo> Claims,
    IReadOnlyCollection<ExistingCharacterInfo> Characters,
    IReadOnlyCollection<ProjectMasterName> Masters,
    UserIdentification DefaultResponsibleMaster,
    DateTimeOffset BackupTakenAt);

/// <summary>Одно действие на заявку.</summary>
public enum ClaimRestoreAction
{
    /// <summary>Заявки нет в восстановленной базе — создать с прежним id.</summary>
    Create,

    /// <summary>Заявка есть, но после бэкапа менялась — привести к состоянию на момент аварии.</summary>
    Change,

    /// <summary>Заявка уже такая, как по уведомлениям.</summary>
    Nothing,
}

/// <summary>В чём план не уверен — мастер проверяет и правит.</summary>
public enum ClaimRestoreWarning
{
    PlayerNotFound,
    PlayerAmbiguous,

    /// <summary>Имя ответственного из уведомлений не совпало ни с одним мастером (или с несколькими).</summary>
    ResponsibleNotFound,

    /// <summary>
    /// Под имя персонажа подходит несколько персонажей проекта — выбрать из
    /// <see cref="ClaimRestorePlanRow.CharacterCandidates"/>; без выбора создавать нельзя.
    /// </summary>
    CharacterAmbiguous,

    /// <summary>Имени персонажа в уведомлениях нет — указать персонажа вручную.</summary>
    CharacterNotFound,

    /// <summary>Заявку перемещали, но персонажа с новым именем в базе нет.</summary>
    MovedCharacterNotFound,

    /// <summary>Имя персонажа другое, а перемещений не было — похоже на переименование, его план не делает.</summary>
    CharacterRenamed,

    /// <summary>
    /// Принятая заявка на слоте: одобрение создаёт из слота нового персонажа. Для потерянной заявки план
    /// создаёт его сам; для заявки из базы — выбрать персонажа вручную.
    /// </summary>
    ApprovedOnSlot,

    /// <summary>По уведомлениям не понять, в каком статусе заявка.</summary>
    StatusUnknown,

    /// <summary>
    /// Заявки нет в базе, но ни события о подаче после бэкапа, ни подачи вовсе — проверить, та ли это
    /// заявка (ошибка момента бэкапа, уведомление о подаче без получателей).
    /// </summary>
    LostClaimNotSubmittedAfterBackup,

    /// <summary>
    /// Заявку правили руками после восстановления: она менялась после аварии, и меняемое значение в базе
    /// не совпадает ни с одной выгрузкой, ни с состоянием по уведомлениям. Такие атрибуты
    /// (<see cref="ClaimRestorePlanRow.AttributesNotByDefault"/>) по умолчанию не применяются.
    /// </summary>
    ChangedAfterRestore,

    /// <summary>
    /// «Итого взнос» в выгрузке заявок меньше ручного взноса из базы или по плану, а после выгрузки
    /// взнос не меняли. Ручной взнос из выгрузки план не ставит — проверить вручную.
    /// </summary>
    TotalFeeMismatch,

    /// <summary>
    /// «Уплачено» в выгрузке заявок свежее бэкапа не совпадает с суммой одобренных оплат в базе (у заявки,
    /// которой нет в базе, оплат нет). Оплаты план не восстанавливает — это подсказка, где суммы не сходятся
    /// (<see cref="ClaimRestorePlanRow.FeePaidMismatch"/>).
    /// </summary>
    FeePaidMismatch,
}

/// <summary>Расхождение уплаченного: «Уплачено» в выгрузке и сумма одобренных оплат в базе.</summary>
public record FeePaidMismatch(int Exported, int InDatabase);

/// <summary>Меняемый атрибут заявки.</summary>
public enum ClaimRestoreAttributeKind
{
    Status,
    ResponsibleMaster,
    Character,
    ManualFee,
}

/// <summary>Спорные атрибуты заявки: значение, откуда оно и чем отличаются другие источники.</summary>
/// <param name="CharacterName">Имя персонажа заявки — по нему план ищет персонажа.</param>
public record ClaimRestoreAttributes(
    RestoreAttribute<ClaimStatus>? Status,
    RestoreAttribute<UserIdentification>? ResponsibleMaster,
    RestoreAttribute<string>? CharacterName);

/// <summary>
/// Строка плана. Для <see cref="ClaimRestoreAction.Change"/> заполнено только то, что меняется;
/// null — оставить как есть.
/// </summary>
/// <param name="CharacterName">Имя персонажа по уведомлениям (или по выгрузке, если уведомлений после неё нет).</param>
/// <param name="Character">
/// Создать: найденный по имени персонаж из базы (или создаваемый планом с прежним id); null — создать нового
/// с именем <paramref name="CharacterName"/> (несколько строк с одним таким именем — один новый персонаж:
/// следующие подавать на созданного первой).
/// Изменить: персонаж, на которого заявку переместили.
/// </param>
/// <param name="CharacterCandidates">Подходящие по имени персонажи, если однозначного нет.</param>
public record ClaimRestorePlanRow(
    ClaimIdentification ClaimId,
    ClaimRestoreAction Action,
    string? CharacterName,
    CharacterIdentification? Character,
    IReadOnlyCollection<CharacterIdentification> CharacterCandidates,
    UserIdentification? Player,
    IReadOnlyCollection<UserIdentification> PlayerCandidates,
    ClaimStatus? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? ResponsibleMaster,
    int? ManualFee,
    DateTimeOffset FirstActivity,
    DateTimeOffset LastActivity,
    IReadOnlyCollection<ClaimRestoreWarning> Warnings)
{
    /// <summary>Применять ли строку, если мастер ничего не выбрал.</summary>
    public bool ApplyByDefault { get; init; } = true;

    /// <summary>Источники спорных атрибутов — для показа расхождений.</summary>
    public ClaimRestoreAttributes? Attributes { get; init; }

    /// <summary>
    /// Меняемые атрибуты, которые по умолчанию не применяются: их, похоже, правили руками после восстановления
    /// (<see cref="ClaimRestoreWarning.ChangedAfterRestore"/>). Остальные изменения строки применяются.
    /// </summary>
    public IReadOnlyCollection<ClaimRestoreAttributeKind> AttributesNotByDefault { get; init; } = [];

    /// <summary>Суммы при <see cref="ClaimRestoreWarning.FeePaidMismatch"/>; null — совпали или сверять не с чем.</summary>
    public FeePaidMismatch? FeePaidMismatch { get; init; }

    /// <summary>
    /// Создание, которое нельзя применить без решения мастера: не выбран игрок, персонаж неоднозначен
    /// или не найден, статус неизвестен.
    /// </summary>
    public bool NeedsManualChoice => Action == ClaimRestoreAction.Create
        && (Player is null
            || CharacterCandidates.Count > 0
            || Warnings.Any(w => w is ClaimRestoreWarning.CharacterNotFound or ClaimRestoreWarning.CharacterAmbiguous or ClaimRestoreWarning.StatusUnknown));
}
