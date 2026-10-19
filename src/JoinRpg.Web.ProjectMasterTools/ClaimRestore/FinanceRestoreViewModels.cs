using System.ComponentModel.DataAnnotations;

namespace JoinRpg.Web.ProjectMasterTools.ClaimRestore;

/// <summary>Что требует внимания в плане восстановления ручных оплат заявки (DRP, #5631).</summary>
public enum FinanceRestoreWarningView
{
    [Display(Name = "Заявки нет в базе — сначала восстановите заявки выше")]
    ClaimNotFound,

    [Display(Name = "Номера части операций в базе заняты другим — эти операции не восстановятся")]
    IdConflict,

    [Display(Name = "Сумма части операций не разобралась — эти операции не восстановятся, проверьте вручную")]
    InvalidMoney,

    [Display(Name = "Тип оплаты части операций не определён — выберите его вручную")]
    PaymentTypeNotDetermined,

    [Display(Name = "Подтверждение или отклонение не удалось связать с операцией — проверьте вручную")]
    ModerationTargetNotFound,

    [Display(Name = "Непонятно, какую операцию подтвердили или отклонили — они останутся ждать подтверждения, проверьте вручную")]
    ModerationAmbiguous,

    [Display(Name = "Подтверждение или отклонение относится к операции, которая не восстановится — проверьте вручную")]
    ModerationOfNotRestoredOperation,

    [Display(Name = "Было решение по льготному взносу — оно восстановится комментарием, саму льготу проверьте вручную")]
    PreferentialModeration,

    [Display(Name = "Были переводы между заявками — они не восстанавливаются, внесите их вручную")]
    TransfersNotRestored,

    [Display(Name = "Часть уведомлений по заявке не разобралась — возможно, потеряна операция, проверьте вручную")]
    UnparsedEvents,
}

/// <summary>Состояние операции после восстановления (с учётом найденного подтверждения или отклонения).</summary>
public enum FinanceRestoreOperationStateView
{
    Approved,
    Proposed,
    Declined,
}

/// <summary>Тип оплаты, который мастер может выбрать для восстанавливаемой операции.</summary>
/// <param name="Name">Название типа.</param>
/// <param name="OwnerName">Кому идут деньги — владелец типа.</param>
/// <param name="IsActive">Тип не выключен. Выключенный в списке, только если его предложил планировщик.</param>
public record FinanceRestorePaymentTypeView(int PaymentTypeId, string Name, string OwnerName, bool IsActive);

/// <summary>Ручная оплата или возврат взноса из уведомления.</summary>
/// <param name="Key">
/// Стабильный ключ операции: по нему остров запоминает выбранный тип между перестроениями плана и присылает
/// выбор в <see cref="FinanceRestoreApplyRequest.PaymentTypes"/>.
/// </param>
/// <param name="OperationId">Прежний номер операции; null — в уведомлении его не было.</param>
/// <param name="IsRefund">Возврат взноса; иначе оплата.</param>
/// <param name="Money">Сумма без знака (у возврата тоже положительная).</param>
/// <param name="CreatedAt">Время уведомления — оно же дата операции.</param>
/// <param name="IsByPlayer">Об оплате сообщил игрок.</param>
/// <param name="SuggestedPaymentTypeId">Тип, предложенный планировщиком; null — не определён.</param>
/// <param name="CandidatePaymentTypeIds">Из каких типов выбирать, если планировщик не смог выбрать один; иначе пусто.</param>
/// <param name="CanApply">Операция восстановится, если выбран тип. False — уже есть, номер занят и т.п.</param>
/// <param name="NotApplicableReason">Почему не восстановится (при <paramref name="CanApply"/> = false), текст для мастера.</param>
public record FinanceRestoreOperationView(
    string Key,
    int? OperationId,
    bool IsRefund,
    int Money,
    DateTimeOffset CreatedAt,
    string AuthorName,
    bool IsByPlayer,
    FinanceRestoreOperationStateView State,
    int? SuggestedPaymentTypeId,
    IReadOnlyList<int> CandidatePaymentTypeIds,
    bool CanApply,
    string? NotApplicableReason);

/// <summary>Вид операции или решения, на которое ссылается текст для мастера.</summary>
public enum FinanceRestoreEntryKind
{
    Payment,
    Refund,
    Approval,
    Rejection,
}

/// <summary>
/// Операция или решение, на которое ссылается текст для мастера. Описание («оплата 500 от …») собирает
/// панель — чтобы время было в том же поясе, что и в таблице.
/// </summary>
/// <param name="Money">Сумма без знака; у решения — null.</param>
public record FinanceRestoreEntryRef(FinanceRestoreEntryKind Kind, int? Money, DateTimeOffset CreatedAt);

/// <summary>Что не передано на восстановление в этот раз.</summary>
/// <param name="Reason">Почему — продолжение описания записи, текст для мастера.</param>
public record FinanceRestoreSkippedView(FinanceRestoreEntryRef Entry, string Reason);

/// <summary>Подтверждение или отклонение финансовой операции из уведомления.</summary>
/// <param name="CommentId">Прежний номер комментария; null — в уведомлении его не было.</param>
/// <param name="Approved">Подтверждение; иначе отклонение.</param>
/// <param name="CanApply">Восстановится при применении заявки (не «уже есть», не конфликт).</param>
/// <param name="Note">
/// Пояснение для мастера: к какой операции относится, почему не восстановится и т.п. Null, если задано
/// <paramref name="TargetOperation"/>.
/// </param>
/// <param name="TargetOperation">Операция из уведомлений, вместе с которой восстановится решение.</param>
public record FinanceRestoreModerationView(
    string Key,
    int? CommentId,
    bool Approved,
    DateTimeOffset CreatedAt,
    string AuthorName,
    bool CanApply,
    string? Note,
    FinanceRestoreEntryRef? TargetOperation = null);

/// <summary>План восстановления ручных оплат одной заявки.</summary>
/// <param name="ClaimExists">Заявка есть в базе; если нет — восстанавливать некуда.</param>
/// <param name="CharacterName">Имя персонажа в базе; null — заявки нет.</param>
/// <param name="PlayerName">Игрок заявки; null — заявки нет.</param>
/// <param name="Operations">Оплаты и возвраты, по времени (в том числе те, что не восстановятся).</param>
/// <param name="Moderations">Подтверждения и отклонения, по времени.</param>
/// <param name="AlreadyExisting">Сколько операций и модераций уже есть в базе.</param>
/// <param name="Conflicts">Сколько номеров в базе занято другим.</param>
/// <param name="Transfers">Сколько переводов между заявками — не восстанавливаются.</param>
/// <param name="OnlinePayments">Сколько онлайн-оплат — восстанавливаются по выгрузке провайдера, не здесь.</param>
/// <param name="Unparsed">Сколько уведомлений по заявке не разобралось.</param>
public record FinanceRestoreRowViewModel(
    ClaimIdentification ClaimId,
    bool ClaimExists,
    string? CharacterName,
    string? PlayerName,
    IReadOnlyList<FinanceRestoreOperationView> Operations,
    IReadOnlyList<FinanceRestoreModerationView> Moderations,
    int AlreadyExisting,
    int Conflicts,
    int Transfers,
    int OnlinePayments,
    int Unparsed,
    IReadOnlyList<FinanceRestoreWarningView> Warnings);

/// <summary>План восстановления ручных оплат проекта. Строки — в порядке номеров заявок.</summary>
/// <param name="UnavailableReason">Не null — восстановление недоступно, строк нет.</param>
/// <param name="PaymentTypes">Неонлайн-типы проекта для выбора: активные и выключенные, если их предложил планировщик.</param>
/// <param name="HasOwnCash">У текущего мастера уже есть тип «наличные».</param>
public record FinanceRestorePlanViewModel(
    ProjectIdentification ProjectId,
    ClaimRestoreUnavailableReason? UnavailableReason,
    IReadOnlyList<FinanceRestorePaymentTypeView> PaymentTypes,
    IReadOnlyList<FinanceRestoreRowViewModel> Rows,
    bool HasOwnCash);

/// <summary>Восстановить ручные оплаты одной заявки.</summary>
/// <param name="PaymentTypes">
/// Ключ операции (<see cref="FinanceRestoreOperationView.Key"/>) → id выбранного типа оплаты. Только операции,
/// для которых мастер выбрал тип; остальные операции заявки не восстанавливаются.
/// </param>
public record FinanceRestoreApplyRequest(ClaimIdentification ClaimId, IReadOnlyDictionary<string, int> PaymentTypes);

/// <summary>Итог восстановления ручных оплат одной заявки.</summary>
/// <param name="Inserted">Сколько операций и модераций вставлено.</param>
/// <param name="AlreadyExisted">Сколько уже было в базе.</param>
/// <param name="Conflicts">Сколько не вставлено: номер занят другим.</param>
/// <param name="ParentsNotRestored">
/// Сколько решений не вставлено: их операция не вставилась (номер заняли между построением плана и вставкой).
/// </param>
/// <param name="Skipped">Что пропущено и почему.</param>
public record FinanceRestoreApplyResult(
    int Inserted,
    int AlreadyExisted,
    int Conflicts,
    int ParentsNotRestored,
    IReadOnlyList<FinanceRestoreSkippedView> Skipped);
