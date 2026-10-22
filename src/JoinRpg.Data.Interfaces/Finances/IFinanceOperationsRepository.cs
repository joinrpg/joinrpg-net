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
}

/// <summary>
/// Возврат, который JoinRpg провела по исходной операции.
/// </summary>
/// <param name="RefundedOperationId">Идентификатор возвращённой операции (<c>FinanceOperation.RefundedOperationId</c>).</param>
/// <param name="RefundId">Идентификатор самой операции возврата.</param>
/// <param name="OperationDate">Дата операции возврата (<c>FinanceOperation.OperationDate</c>).</param>
public record ApprovedRefund(int RefundedOperationId, FinanceOperationIdentification RefundId, DateTime OperationDate);
