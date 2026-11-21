using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.Interfaces;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// Чтение одобренных денежных операций поверх заданного списка (виджет бюджета). Метод
/// <see cref="GetApprovedMoneyOperations"/> фильтрует по дате, как боевой репозиторий, и
/// запоминает запрошенную дату (<see cref="RequestedFrom"/>) — тесты сверяют её с Start
/// завершённого года; остальные методы сознательно не поддержаны (по образцу FakeUnitOfWork).
/// </summary>
internal sealed class FakeMoneyOperationsRepository(
    IReadOnlyCollection<ApprovedMoneyOperation> operations) : IFinanceOperationsRepository
{
    /// <summary>Последняя дата, с которой сервис запросил операции.</summary>
    public DateOnly? RequestedFrom { get; private set; }

    public Task<IReadOnlyCollection<ApprovedMoneyOperation>> GetApprovedMoneyOperations(ProjectIdentification projectId, DateOnly from)
    {
        RequestedFrom = from;
        return Task.FromResult<IReadOnlyCollection<ApprovedMoneyOperation>>([.. operations.Where(o => o.Date >= from)]);
    }

    public Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination? pagination = null)
        => throw new NotSupportedException();

    public Task<IReadOnlyCollection<ApprovedRefund>> GetApprovedRefunds(IReadOnlyCollection<int> refundedOperationIds)
        => throw new NotSupportedException();

    public Task<IReadOnlyCollection<MatchedOperation>> GetOperationsByCommentIds(IReadOnlyCollection<int> commentIds)
        => throw new NotSupportedException();
}
