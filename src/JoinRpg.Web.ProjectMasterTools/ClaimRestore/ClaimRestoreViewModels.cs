using System.ComponentModel.DataAnnotations;
using JoinRpg.Web.Claims;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>Что сделать с заявкой при восстановлении (DRP, #5481).</summary>
public enum ClaimRestoreActionView
{
    [Display(Name = "Создать")]
    Create,

    [Display(Name = "Изменить")]
    Change,

    [Display(Name = "Ничего")]
    Nothing,
}

/// <summary>В чём план не уверен — мастер проверяет и правит.</summary>
public enum ClaimRestoreWarningView
{
    [Display(Name = "Игрок не найден")]
    PlayerNotFound,

    [Display(Name = "Под имя игрока подходит несколько пользователей")]
    PlayerAmbiguous,

    [Display(Name = "Ответственный мастер из уведомлений не найден")]
    ResponsibleNotFound,

    [Display(Name = "Под имя персонажа подходит несколько персонажей — выберите")]
    CharacterAmbiguous,

    [Display(Name = "В уведомлениях нет имени персонажа — укажите персонажа")]
    CharacterNotFound,

    [Display(Name = "Заявку перемещали, но персонажа с новым именем нет")]
    MovedCharacterNotFound,

    [Display(Name = "Имя персонажа другое — похоже на переименование, его план не делает")]
    CharacterRenamed,

    [Display(Name = "Принятая заявка на слоте")]
    ApprovedOnSlot,

    [Display(Name = "Статус заявки по уведомлениям не понять")]
    StatusUnknown,

    [Display(Name = "Подачи после бэкапа не видно — проверьте, та ли это заявка")]
    LostClaimNotSubmittedAfterBackup,

    [Display(Name = "Изменено после восстановления — проверьте, не ручная ли это правка")]
    ChangedAfterRestore,

    [Display(Name = "Итоговый взнос в выгрузке меньше ручного взноса")]
    TotalFeeMismatch,
}

/// <summary>Почему восстановление недоступно.</summary>
public enum ClaimRestoreUnavailableReason
{
    NoAccess,
    ProjectNotBlocked,
}

public record ClaimRestoreCharacterOption(CharacterIdentification CharacterId, string Name);

public record ClaimRestoreMasterOption(UserIdentification UserId, string Name);

/// <summary>
/// Строка плана восстановления. Для <see cref="ClaimRestoreActionView.Change"/> предлагаемое заполнено
/// только там, где что-то меняется; null — оставить как есть. Рядом — как сейчас в базе, чтобы мастер видел разницу.
/// </summary>
/// <param name="CharacterName">Имя персонажа по уведомлениям.</param>
/// <param name="CurrentCharacter">Персонаж, на котором заявка сейчас (только для заявок из базы).</param>
/// <param name="Character">
/// Создать: найденный по имени персонаж; null — создать нового с именем <paramref name="CharacterName"/>.
/// Изменить: персонаж, на которого заявку переместили.
/// </param>
public record ClaimRestoreRowViewModel(
    ClaimIdentification ClaimId,
    ClaimRestoreActionView Action,
    string? CharacterName,
    ClaimRestoreCharacterOption? CurrentCharacter,
    ClaimRestoreCharacterOption? Character,
    IReadOnlyList<ClaimRestoreCharacterOption> CharacterCandidates,
    UserLinkViewModel? Player,
    IReadOnlyList<UserLinkViewModel> PlayerCandidates,
    ClaimStatusView? CurrentStatus,
    ClaimStatusView? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? CurrentResponsibleMaster,
    UserIdentification? ResponsibleMaster,
    int? CurrentFee,
    int? ManualFee,
    DateTimeOffset FirstActivity,
    DateTimeOffset LastActivity,
    IReadOnlyList<ClaimRestoreWarningView> Warnings);

/// <summary>План восстановления заявок проекта. Строки — в порядке применения.</summary>
/// <param name="UnavailableReason">Не null — восстановление недоступно, строк нет.</param>
public record ClaimRestorePlanViewModel(
    ProjectIdentification ProjectId,
    ClaimRestoreUnavailableReason? UnavailableReason,
    DateTimeOffset BackupTakenAt,
    DateTimeOffset LostAt,
    IReadOnlyList<ClaimRestoreRowViewModel> Rows,
    IReadOnlyList<ClaimRestoreMasterOption> Masters);

/// <summary>Применить одну строку плана — с правками мастера.</summary>
/// <param name="Character">Создать: существующий персонаж; null — новый с именем <paramref name="NewCharacterName"/>. Изменить: куда переместить.</param>
/// <param name="Player">Игрок новой заявки; null — взять из <paramref name="PlayerLink"/>.</param>
/// <param name="PlayerLink">Ссылка на профиль или id игрока, если мастер указал его вручную.</param>
/// <param name="CreatedAt">Когда заявка была подана — первое событие по ней.</param>
public record ClaimRestoreApplyRequest(
    ClaimIdentification ClaimId,
    ClaimRestoreActionView Action,
    CharacterIdentification? Character,
    string? NewCharacterName,
    UserIdentification? Player,
    string? PlayerLink,
    ClaimStatusView? Status,
    DateTimeOffset? StatusChangedAt,
    UserIdentification? ResponsibleMaster,
    int? ManualFee,
    DateTimeOffset CreatedAt);

/// <param name="Character">Персонаж заявки после применения (для «Создать» — найденный или созданный).</param>
public record ClaimRestoreApplyResult(CharacterIdentification? Character);
