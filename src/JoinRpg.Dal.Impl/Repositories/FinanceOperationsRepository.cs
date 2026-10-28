using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DomainTypes.ProjectMetadata.Payments;
using JoinRpg.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

internal class FinanceOperationsRepository(MyDbContext ctx) : IFinanceOperationsRepository
{
    /// <summary>
    /// Грузит операции, которые не перешли в финальный статус
    /// </summary>
    public async Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination)
    {
        var query = ctx.Set<FinanceOperation>()
                .AsExpandable()
                .Where(fo => fo.State != FinanceOperationState.Declined && fo.State != FinanceOperationState.Approved)
                .Where(fo => fo.OperationType == FinanceOperationType.Online)
                // Платежи заблокированного проекта (ADR023) сверяют с провайдером при восстановлении.
                .Where(fo => !fo.Project.IsBlocked)
                .ApplyPaginationEf(pagination, x => x.CommentId)
                .Select(fo => new { fo.ProjectId, fo.ClaimId, fo.CommentId });

        var result = await query.ToArrayAsync();
        return [.. result.Select(r => new FinanceOperationIdentification(r.ProjectId, r.ClaimId, r.CommentId))];
    }

    /// <summary>
    /// Проведённые возвраты по заданным операциям
    /// </summary>
    /// <remarks>
    /// Только <see cref="FinanceOperationState.Approved"/> — тот же критерий, по которому
    /// <c>PaymentsService</c> проверяет, возвращена ли операция уже: возврат, который предложен,
    /// но не подтверждён банком, деньгами ещё не стал.
    /// </remarks>
    public async Task<IReadOnlyCollection<ApprovedRefund>> GetApprovedRefunds(IReadOnlyCollection<int> refundedOperationIds)
    {
        if (refundedOperationIds.Count == 0)
        {
            return [];
        }

        var query = ctx.Set<FinanceOperation>()
                .Where(fo => fo.OperationType == FinanceOperationType.Refund)
                .Where(fo => fo.State == FinanceOperationState.Approved)
                .Where(fo => fo.RefundedOperationId != null && refundedOperationIds.Contains(fo.RefundedOperationId.Value))
                .Select(fo => new { fo.RefundedOperationId, fo.ProjectId, fo.ClaimId, fo.CommentId, fo.OperationDate });

        var result = await query.ToArrayAsync();
        return [.. result.Select(r => new ApprovedRefund(
            r.RefundedOperationId!.Value,
            new FinanceOperationIdentification(r.ProjectId, r.ClaimId, r.CommentId),
            r.OperationDate))];
    }

    /// <summary>
    /// Операции по их идентификаторам
    /// </summary>
    /// <remarks>
    /// Идентификатор операции — это <c>CommentId</c>, он же её первичный ключ, поэтому соединение
    /// со списком банка идёт по числу и одним запросом на весь список.
    /// </remarks>
    public async Task<IReadOnlyCollection<MatchedOperation>> GetOperationsByCommentIds(IReadOnlyCollection<int> commentIds)
    {
        if (commentIds.Count == 0)
        {
            return [];
        }

        var query = ctx.Set<FinanceOperation>()
                .Where(fo => commentIds.Contains(fo.CommentId))
                .Select(fo => new { fo.ProjectId, fo.ClaimId, fo.CommentId, fo.State, fo.OperationType, fo.PaymentTypeId, fo.OperationDate });

        var result = await query.ToArrayAsync();
        return [.. result.Select(r => new MatchedOperation(
            new FinanceOperationIdentification(r.ProjectId, r.ClaimId, r.CommentId),
            r.State,
            r.OperationType,
            r.PaymentTypeId,
            r.OperationDate))];
    }

    /// <summary>
    /// Готовность проектов принять восстановленный онлайн-платёж
    /// </summary>
    /// <remarks>
    /// Типы онлайн-оплаты выбираются подзапросом к той же строке проекта, поэтому на весь набор
    /// проектов один поход в базу. Отдаются и выключенные типы: платёж случился, когда онлайн-оплата
    /// была включена, и тип мог быть удалён с тех пор (тип оплаты удаляется только мягко).
    /// </remarks>
    public async Task<IReadOnlyCollection<ProjectPaymentsReadiness>> GetPaymentsReadiness(IReadOnlyCollection<int> projectIds)
    {
        if (projectIds.Count == 0)
        {
            return [];
        }

        var query = ctx.Set<Project>()
                .Where(project => projectIds.Contains(project.ProjectId))
                .Select(project => new
                {
                    project.ProjectId,
                    project.IsBlocked,
                    OnlinePaymentTypes = project.PaymentTypes
                        .Where(paymentType => paymentType.TypeKind == PaymentTypeKind.Online)
                        .Select(paymentType => new { paymentType.PaymentTypeId, paymentType.IsActive })
                        .ToList(),
                });

        var result = await query.ToArrayAsync();
        return [.. result.Select(r => new ProjectPaymentsReadiness(
            r.ProjectId,
            r.IsBlocked,
            [.. r.OnlinePaymentTypes.Select(paymentType => new OnlinePaymentType(paymentType.PaymentTypeId, paymentType.IsActive))]))];
    }
}
