using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Services.Drp.ClaimNotifications;

namespace JoinRpg.Services.Drp.ManualFinance;

/// <summary>Финансовая операция заявки в восстановленной базе.</summary>
/// <param name="OperationId">Id операции — он же id её комментария.</param>
/// <param name="CreatedAt">
/// Когда операция создана (<c>FinanceOperation.Created</c>; у восстановленной — время уведомления), не дата операции:
/// с ним сравнивается время уведомления.
/// </param>
public record ExistingFinanceOperationInfo(
    int OperationId,
    ClaimIdentification ClaimId,
    FinanceOperationType Type,
    FinanceOperationState State,
    int Money,
    DateTimeOffset CreatedAt);

/// <summary>
/// Финансовый комментарий заявки в восстановленной базе: с действием <see cref="CommentExtraAction.PaidFee"/>,
/// <see cref="CommentExtraAction.RefundFee"/>, <see cref="CommentExtraAction.ApproveFinance"/> или
/// <see cref="CommentExtraAction.RejectFinance"/>.
/// </summary>
public record ExistingFinanceCommentInfo(
    int CommentId,
    ClaimIdentification ClaimId,
    UserIdentification Author,
    DateTimeOffset CreatedAt,
    CommentExtraAction ExtraAction);

/// <summary>Тип оплаты проекта.</summary>
/// <param name="IsActive">Тип не отключён.</param>
public record ProjectPaymentTypeInfo(
    int PaymentTypeId,
    PaymentTypeKind Kind,
    UserIdentification Owner,
    bool IsActive);

/// <summary>Всё, по чему строится план восстановления ручных финансовых операций одного проекта (#5561).</summary>
/// <param name="Events">События по заявкам проекта (<see cref="ClaimNotificationParser"/>).</param>
/// <param name="Claims">Заявки проекта, которые есть в базе.</param>
/// <param name="ExistingOperations">Финансовые операции этих заявок, которые есть в базе.</param>
/// <param name="ExistingComments">Финансовые комментарии этих заявок, которые есть в базе.</param>
/// <param name="TakenCommentIds">
/// Все id из уведомлений, которые в базе заняты чем-либо, кроме <paramref name="ExistingComments"/> и
/// <paramref name="ExistingOperations"/>: в том числе нефинансовыми комментариями той же заявки, чужим
/// обсуждением, форумом, другим проектом. С таким id операцию или модерацию вставить нельзя.
/// </param>
/// <param name="PaymentTypes">Типы оплаты проекта — для предложения типа восстановленной операции.</param>
/// <param name="BackupTakenAt">Момент бэкапа: события до него в восстановленной базе уже есть.</param>
public record FinanceRestoreSource(
    IReadOnlyCollection<ClaimNotificationEvent> Events,
    IReadOnlyCollection<ClaimIdentification> Claims,
    IReadOnlyCollection<ExistingFinanceOperationInfo> ExistingOperations,
    IReadOnlyCollection<ExistingFinanceCommentInfo> ExistingComments,
    IReadOnlyCollection<int> TakenCommentIds,
    IReadOnlyCollection<ProjectPaymentTypeInfo> PaymentTypes,
    DateTimeOffset BackupTakenAt);

/// <summary>Что делать со строкой плана.</summary>
public enum FinanceRestoreStatus
{
    /// <summary>В базе нет — вставить.</summary>
    Restore,

    /// <summary>Уже есть в базе (по id или, без id, по времени и содержанию) — ничего не делать.</summary>
    AlreadyExists,

    /// <summary>Id из уведомления в базе занят чем-то другим — не вставляется.</summary>
    Conflict,

    /// <summary>Суммы нет, она нулевая или её знак не совпадает с действием — не вставляется.</summary>
    InvalidMoney,
}

/// <summary>Как подобран тип оплаты операции.</summary>
public enum PaymentTypeGuess
{
    /// <summary>Тип, владелец которого — инициатор операции.</summary>
    Initiator,

    /// <summary>Тип, владелец которого среди получателей уведомления (владелец получает его всегда, кроме своих операций).</summary>
    Recipient,

    /// <summary>Единственный неонлайн-тип проекта.</summary>
    SingleInProject,

    /// <summary>Неонлайн-типов в проекте нет, операцию отметил мастер — создать тип «наличные» на него.</summary>
    CreateCash,

    /// <summary>Подходит несколько типов (<see cref="PaymentTypeSuggestion.Candidates"/>) — выбрать вручную.</summary>
    Ambiguous,

    /// <summary>Тип не определён — без ручного выбора операция не применяется.</summary>
    NotFound,
}

/// <summary>Предложенный тип оплаты: заполнено одно из <paramref name="PaymentTypeId"/> и <paramref name="CreateCashFor"/> или ни одного.</summary>
/// <param name="PaymentTypeId">Существующий тип оплаты.</param>
/// <param name="CreateCashFor">Создать тип «наличные» на этого пользователя (<see cref="PaymentTypeGuess.CreateCash"/>).</param>
/// <param name="Candidates">Из каких типов выбирать при <see cref="PaymentTypeGuess.Ambiguous"/>; иначе пусто.</param>
public record PaymentTypeSuggestion(
    int? PaymentTypeId,
    UserIdentification? CreateCashFor,
    PaymentTypeGuess How,
    IReadOnlyCollection<int> Candidates);

/// <summary>Ручная оплата или возврат взноса из уведомления.</summary>
/// <param name="Key">
/// Стабильный ключ для сопоставления строк CSV при повторном построении плана: <c>id:{OperationId}</c>,
/// если id известен, иначе по заявке, времени и инициатору.
/// </param>
/// <param name="OperationId">Прежний id операции (= id комментария); null — в уведомлении его не было (до 2026-06-06).</param>
/// <param name="IsByPlayer">Игрок сообщил об оплате — операция заявленная, до модерации <see cref="FinanceOperationState.Proposed"/>.</param>
/// <param name="Type"><see cref="FinanceOperationType.Submit"/> для оплаты, <see cref="FinanceOperationType.Refund"/> для возврата.</param>
/// <param name="Money">Сумма со знаком, как в уведомлении: у возврата отрицательная.</param>
/// <param name="CreatedAt">Время уведомления — и дата операции: своей даты операции в уведомлении нет.</param>
/// <param name="State">Состояние с учётом найденной модерации.</param>
/// <param name="ModeratedAt">Когда операцию подтвердили или отклонили, если модерация с ней связалась.</param>
public record FinanceOperationToRestore(
    string Key,
    int? OperationId,
    UserIdentification Initiator,
    bool IsByPlayer,
    FinanceOperationType Type,
    int? Money,
    DateTimeOffset CreatedAt,
    string Text,
    CommentExtraAction ExtraAction,
    FinanceOperationState State,
    DateTimeOffset? ModeratedAt,
    PaymentTypeSuggestion PaymentType,
    FinanceRestoreStatus Status);

/// <summary>С чем связалась модерация. В уведомлении о модерации операция не указана — связь ищется по заявке и времени.</summary>
public enum FinanceModerationLink
{
    /// <summary>С восстанавливаемой операцией (<see cref="FinanceModerationToRestore.TargetOperationKey"/>): её состояние уже учтено в плане.</summary>
    RestoredOperation,

    /// <summary>
    /// С операцией, которая есть в базе (<see cref="FinanceModerationToRestore.TargetOperationId"/>): заявленной
    /// строкой базы или операцией игрока из уведомлений, совпавшей с базой (тогда есть и
    /// <see cref="FinanceModerationToRestore.TargetOperationKey"/>, а её состояние в плане уже учтено — в любом
    /// состоянии в базе). Если она ещё заявленная, новое состояние ей ставит применение плана.
    /// </summary>
    ExistingOperation,

    /// <summary>
    /// С операцией из уведомлений, которая не вставляется (<see cref="FinanceRestoreStatus.Conflict"/>,
    /// <see cref="FinanceRestoreStatus.InvalidMoney"/>; ключ — <see cref="FinanceModerationToRestore.TargetOperationKey"/>):
    /// состояние ничьё не меняется, разобрать вручную.
    /// </summary>
    NotRestoredOperation,

    /// <summary>
    /// С запросом льготного взноса — из базы (<see cref="FinanceModerationToRestore.TargetOperationId"/>) или из
    /// уведомления (не восстанавливается, id нет). Модерация восстанавливается, состояние и льгота не меняются.
    /// </summary>
    Preferential,

    /// <summary>Подходящей заявленной операции раньше модерации нет.</summary>
    NotFound,

    /// <summary>Подходящих операций несколько — какую модерировали, не понять.</summary>
    Ambiguous,
}

/// <summary>Подтверждение или отклонение финансовой операции из уведомления.</summary>
/// <param name="Key">Ключ для CSV: <c>id:{CommentId}</c>, если id известен, иначе по заявке, времени и инициатору.</param>
/// <param name="CommentId">Прежний id комментария модерации; null — в уведомлении его не было.</param>
/// <param name="Approved">Подтверждение (<see cref="CommentExtraAction.ApproveFinance"/>) или отклонение.</param>
/// <param name="TargetOperationKey">
/// Ключ операции из уведомлений, с которой связалась модерация (<see cref="FinanceModerationLink.RestoredOperation"/>,
/// <see cref="FinanceModerationLink.NotRestoredOperation"/>, <see cref="FinanceModerationLink.ExistingOperation"/>).
/// </param>
/// <param name="TargetOperationId">Id операции из базы при <see cref="FinanceModerationLink.ExistingOperation"/> и льготе из базы.</param>
public record FinanceModerationToRestore(
    string Key,
    int? CommentId,
    UserIdentification Initiator,
    bool Approved,
    DateTimeOffset CreatedAt,
    string Text,
    FinanceModerationLink Link,
    string? TargetOperationKey,
    int? TargetOperationId,
    FinanceRestoreStatus Status);

/// <summary>Что требует внимания в плане заявки.</summary>
public enum FinanceRestoreWarning
{
    /// <summary>Заявки нет в базе — сначала восстановить заявки.</summary>
    ClaimNotFound,

    /// <summary>Id операции или модерации в базе занят другим (<see cref="FinanceRestoreStatus.Conflict"/>).</summary>
    IdConflict,

    /// <summary>Сумма не разобрана или не сходится с действием (<see cref="FinanceRestoreStatus.InvalidMoney"/>).</summary>
    InvalidMoney,

    /// <summary>Для восстанавливаемой операции тип оплаты не определён или неоднозначен — выбрать вручную.</summary>
    PaymentTypeNotDetermined,

    /// <summary>Модерация не связалась ни с одной операцией: подходящих нет.</summary>
    ModerationTargetNotFound,

    /// <summary>Модерация не связалась: подходящих операций несколько, они остаются заявленными.</summary>
    ModerationAmbiguous,

    /// <summary>Модерация связалась с операцией, которая не вставляется (<see cref="FinanceModerationLink.NotRestoredOperation"/>).</summary>
    ModerationOfNotRestoredOperation,

    /// <summary>Модерация запроса льготного взноса — восстанавливается, но состояние и льгота не меняются.</summary>
    PreferentialModeration,

    /// <summary>Были переводы между заявками (<see cref="FinanceRestorePlanRow.Transfers"/>) — суммы нет, не восстанавливаются.</summary>
    TransfersNotRestored,

    /// <summary>Есть уведомления по заявке, которые не разобрались (<see cref="FinanceRestorePlanRow.Unparsed"/>).</summary>
    UnparsedEvents,
}

/// <summary>План восстановления ручных финансовых операций одной заявки.</summary>
/// <param name="ClaimExists">Заявка есть в базе; если нет — списки пусты, а события только посчитаны.</param>
/// <param name="Operations">Оплаты и возвраты, по времени, со статусом каждой.</param>
/// <param name="Moderations">Подтверждения и отклонения, по времени, со статусом каждого.</param>
/// <param name="SkippedForMissingClaim">Сколько оплат, возвратов и модераций не восстановить, потому что заявки нет.</param>
/// <param name="Transfers">Сколько переводов между заявками — не восстанавливаются, суммы нет.</param>
/// <param name="PreferentialRequests">Сколько запросов льготного взноса — не восстанавливаются.</param>
/// <param name="OnlinePayments">Сколько онлайн-оплат — их восстанавливают по выгрузке провайдера.</param>
/// <param name="Unparsed">Сколько уведомлений по заявке не разобралось.</param>
public record FinanceRestorePlanRow(
    ClaimIdentification ClaimId,
    bool ClaimExists,
    IReadOnlyList<FinanceOperationToRestore> Operations,
    IReadOnlyList<FinanceModerationToRestore> Moderations,
    int SkippedForMissingClaim,
    int Transfers,
    int PreferentialRequests,
    int OnlinePayments,
    int Unparsed,
    IReadOnlyCollection<FinanceRestoreWarning> Warnings)
{
    /// <summary>Сколько операций и модераций уже есть в базе.</summary>
    public int AlreadyExisting
        => Operations.Count(o => o.Status == FinanceRestoreStatus.AlreadyExists)
        + Moderations.Count(m => m.Status == FinanceRestoreStatus.AlreadyExists);

    /// <summary>Id из уведомлений, занятые в базе другим.</summary>
    public IReadOnlyCollection<int> Conflicts
        => [.. Operations.Where(o => o.Status == FinanceRestoreStatus.Conflict).Select(o => o.OperationId!.Value)
            .Concat(Moderations.Where(m => m.Status == FinanceRestoreStatus.Conflict).Select(m => m.CommentId!.Value))];
}
