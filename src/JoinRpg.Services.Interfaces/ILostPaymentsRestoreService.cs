using JoinRpg.DomainTypes.ProjectMetadata.Payments;

namespace JoinRpg.Services.Interfaces;

/// <summary>
/// Восстановление платежа, который банк получил, а JoinRpg не записал (ADR027).
/// </summary>
/// <remarks>
/// <para>
/// Платёж пишется **с прежним номером**: <c>orderId</c> банка это <c>CommentId</c> операции, и
/// восстановленный платёж обязан остаться тем же платежом — тогда повторная сверка находит его
/// по номеру, а будущий возврат обращается в банк с номером, который банк знает. Возврат своего
/// номера в банке не имеет, поэтому пишется новой операцией со ссылкой на исходную, как делает
/// боевой путь.
/// </para>
/// <para>
/// В банк сервис не ходит вовсе: первоисточник — список, который уже скачал шлюз.
/// </para>
/// <para>
/// Обе записи создаются проведёнными (состояние <c>Approved</c>): банк деньги получил, боевой путь
/// приходит к тому же состоянию через сверку. Взнос заявки пересчитывается тем же приёмом, что и при
/// боевом проведении, а уведомление отправляется после записи.
/// </para>
/// </remarks>
public interface ILostPaymentsRestoreService
{
    /// <summary>
    /// Восстанавливает потерянный платёж, а если банк зафиксировал возврат — и возврат следом.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Повторный вызов ничего не вставляет: номер уже занят нашей же операцией, и исход будет
    /// <see cref="LostOperationRestoreStatus.AlreadyRestored"/>.
    /// </para>
    /// <para>
    /// Сбой вставки возврата не уносит уже записанный платёж: взнос пересчитывается на полную сумму,
    /// уведомление о платеже отправляется, а возврат получает исход
    /// <see cref="LostOperationRestoreStatus.Failed"/> с текстом сбоя — повторный прогон восстановит
    /// его как пропущенный возврат.
    /// </para>
    /// </remarks>
    /// <exception cref="ProjectBlockedException">Проект заблокирован (ADR023): его восстанавливают
    /// инструменты DRP, а не этот сервис.</exception>
    Task<LostPaymentRestoreResult> RestorePaymentAsync(LostPaymentToRestore payment);

    /// <summary>
    /// Восстанавливает только возврат: сам платёж в базе есть и проведён.
    /// </summary>
    Task<LostRefundRestoreResult> RestoreRefundAsync(LostRefundToRestore refund);
}

/// <summary>
/// Потерянный платёж, который можно восстановить: плательщик, проект, заявка и тип оплаты
/// определились однозначно.
/// </summary>
/// <param name="OperationId">Прежний номер операции — <c>orderId</c> банка как число.</param>
/// <param name="MoneyAmount">Сумма в целых рублях, положительная.</param>
/// <param name="OperationDate">Дата денег — <c>stateDate</c> банка в UTC.</param>
/// <param name="BankPaymentId">Номер платежа в банке.</param>
/// <param name="PayerComment">Комментарий плательщика из банка; попадает в текст второй строкой.</param>
/// <param name="Refund">Возврат, который банк зафиксировал по этому платежу, либо <c>null</c>.</param>
public record LostPaymentToRestore(
    ClaimIdentification ClaimId,
    int OperationId,
    PaymentTypeIdentification PaymentTypeId,
    int MoneyAmount,
    DateTime OperationDate,
    string BankPaymentId,
    string? PayerComment,
    LostRefundToRestore? Refund);

/// <summary>
/// Возврат, который банк зафиксировал, а JoinRpg не записал.
/// </summary>
/// <param name="RefundedOperationId">Номер операции, которую возвращают: у возврата, восстановленного
/// вместе с платежом, это номер самого платежа.</param>
/// <param name="MoneyAmount">Сумма возврата в целых рублях, положительная; знаком её сделает сервис.</param>
/// <param name="OperationDate">Дата возврата — <c>stateDate</c> банка в UTC.</param>
/// <param name="RefundedOperationDate">Дата исходного платежа: идёт в текст комментария, как в боевом
/// возврате. Для восстанавливаемого вместе с платежом возврата это <c>OperationDate</c> платежа.</param>
/// <param name="BankRefundId">Номер возврата в банке.</param>
public record LostRefundToRestore(
    ClaimIdentification ClaimId,
    int RefundedOperationId,
    PaymentTypeIdentification PaymentTypeId,
    int MoneyAmount,
    DateTime OperationDate,
    DateTime RefundedOperationDate,
    string BankPaymentId,
    string BankRefundId);

/// <summary>Исход восстановления одной записи.</summary>
public enum LostOperationRestoreStatus
{
    /// <summary>Запись создана.</summary>
    Restored,

    /// <summary>Запись уже была — повторный прогон ничего не вставил.</summary>
    AlreadyRestored,

    /// <summary>Номер платежа занят другим комментарием: восстановил человек, не инструмент.</summary>
    NumberTaken,

    /// <summary>Номер не ниже текущего счётчика комментариев, такая вставка сдвинула бы счётчик.</summary>
    NumberNotLost,

    /// <summary>
    /// Вставка не удалась (сбой). Используется для возврата, вставляемого вместе с платёжом, когда
    /// сам платёж уже записан: сбой возврата не уносит платёж, а повторный прогон восстановит
    /// возврат как пропущенный (<c>RefundMissing</c>).
    /// </summary>
    Failed,

    /// <summary>
    /// Операция дописана к уже существующему комментарию заявки — например, возвращённому
    /// инструментами DRP без операции: текст комментария не тронут.
    /// </summary>
    Attached,
}

/// <summary>Исход восстановления возврата.</summary>
/// <param name="OperationId">
/// Номер операции возврата — созданной или уже существовавшей; при <see cref="LostOperationRestoreStatus.Failed"/>
/// — <c>null</c>, записи нет.
/// </param>
/// <param name="FeeRecalculated">Взнос заявки пересчитан и сохранён. <c>false</c> означает, что
/// операция создана, а пересчёт не сохранился: запись в базе есть, и чинить взнос придётся вручную.</param>
/// <param name="FailureMessage">Текст сбоя для отчёта админу; <c>null</c>, если вставка не падала.</param>
public record LostRefundRestoreResult(
    LostOperationRestoreStatus Status,
    int? OperationId,
    bool FeeRecalculated,
    string? FailureMessage = null);

/// <summary>Исход восстановления платежа.</summary>
/// <param name="OperationId">Номер операции платежа — созданный, уже существовавший либо запрошенный.</param>
/// <param name="Refund">Исход возврата; <c>null</c>, если банк возврат не фиксировал либо платёж
/// не был восстановлен.</param>
/// <param name="FeeRecalculated">Взнос заявки пересчитан и сохранён — см. <see cref="LostRefundRestoreResult"/>.</param>
public record LostPaymentRestoreResult(LostOperationRestoreStatus Status, int OperationId, LostRefundRestoreResult? Refund, bool FeeRecalculated);
