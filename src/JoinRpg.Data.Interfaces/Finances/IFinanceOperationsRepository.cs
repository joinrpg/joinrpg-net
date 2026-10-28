using JoinRpg.DomainTypes.Characters.Claims.Finances;
using JoinRpg.Interfaces;

namespace JoinRpg.Data.Interfaces.Finances;

public interface IFinanceOperationsRepository
{
    Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination = null);

    /// <summary>
    /// Проведённые возвраты по заданным операциям — одним запросом на весь набор.
    /// </summary>
    /// <param name="refundedOperationIds">Идентификаторы возвращённых операций (<c>FinanceOperation.CommentId</c>).</param>
    Task<IReadOnlyCollection<ApprovedRefund>> GetApprovedRefunds(IReadOnlyCollection<int> refundedOperationIds);

    /// <summary>
    /// Операции по их идентификаторам — одним запросом на весь набор.
    /// </summary>
    /// <remarks>
    /// Отдаёт и те, что не проведены: сверка списка банка с базой (ADR026) различает «операции нет»
    /// и «операция есть, но не проведена», поэтому состояние и вид операции нужны для любой найденной.
    /// </remarks>
    /// <param name="commentIds">Идентификаторы операций (<c>FinanceOperation.CommentId</c>).</param>
    Task<IReadOnlyCollection<MatchedOperation>> GetOperationsByCommentIds(IReadOnlyCollection<int> commentIds);

    /// <summary>
    /// Готовность проектов принять восстановленный онлайн-платёж — одним запросом на весь набор.
    /// </summary>
    /// <remarks>
    /// Блокировка проекта и его активные онлайн-типы оплаты лежат в одной строке проекта, поэтому
    /// читаются вместе. Типов отдаётся набор, а не один: «нет ни одного» и «несколько» — разные
    /// причины отказа, и админ должен видеть разницу.
    /// </remarks>
    /// <param name="projectIds">Проекты, которые надо проверить.</param>
    Task<IReadOnlyCollection<ProjectPaymentsReadiness>> GetPaymentsReadiness(IReadOnlyCollection<int> projectIds);
}

/// <summary>
/// Возврат, который JoinRpg провела по исходной операции.
/// </summary>
/// <param name="RefundedOperationId">Идентификатор возвращённой операции (<c>FinanceOperation.RefundedOperationId</c>).</param>
/// <param name="RefundId">Идентификатор самой операции возврата.</param>
/// <param name="OperationDate">Дата операции возврата (<c>FinanceOperation.OperationDate</c>).</param>
public record ApprovedRefund(int RefundedOperationId, FinanceOperationIdentification RefundId, DateTime OperationDate);

/// <summary>
/// Финансовая операция, найденная по её идентификатору.
/// </summary>
/// <param name="OperationId">Идентификатор операции; он же даёт проект и заявку.</param>
/// <param name="State">Состояние операции (<c>FinanceOperation.State</c>).</param>
/// <param name="OperationType">Вид операции (<c>FinanceOperation.OperationType</c>).</param>
/// <param name="PaymentTypeId">Тип оплаты операции. Нужен восстановлению возврата: возврат пишется
/// на тип исходного платежа, а не на тот, что активен в проекте сегодня.</param>
/// <param name="OperationDate">Дата денег исходной операции. Идёт в текст комментария восстановленного
/// возврата — так же, как боевой возврат пишет дату возвращаемого платежа.</param>
public record MatchedOperation(
    FinanceOperationIdentification OperationId,
    FinanceOperationState State,
    FinanceOperationType OperationType,
    int? PaymentTypeId,
    DateTime OperationDate);

/// <summary>
/// Готов ли проект принять восстановленный платёж (ADR026).
/// </summary>
/// <param name="ProjectId">Проект.</param>
/// <param name="IsBlocked">Проект заблокирован на время восстановления данных (ADR023) — в нём
/// боевой путь не даёт ни платить, ни проводить платежи.</param>
/// <param name="OnlinePaymentTypes">Типы оплаты проекта с <c>TypeKind = Online</c>, активные
/// и неактивные. Неактивные нужны: потерянный платёж случился, когда онлайн-оплата была включена,
/// и тип мог быть с тех пор выключен.</param>
public record ProjectPaymentsReadiness(int ProjectId, bool IsBlocked, IReadOnlyCollection<OnlinePaymentType> OnlinePaymentTypes);

/// <summary>
/// Тип онлайн-оплаты проекта.
/// </summary>
/// <param name="PaymentTypeId">Номер типа оплаты.</param>
/// <param name="IsActive">Тип не выключен. Тип оплаты не удаляется физически никогда, поэтому
/// <c>false</c> означает «был удалён» (soft-delete).</param>
public record OnlinePaymentType(int PaymentTypeId, bool IsActive);
