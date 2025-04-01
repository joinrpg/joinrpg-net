using System.Data.Entity;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Finances;
using JoinRpg.DataModel;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;
internal class FinanceOperationsRepository(MyDbContext ctx) : RepositoryImplBase(ctx), IFinanceOperationsRepository
{
    /// <summary>
    /// Грузит операции, которые не перешли в финальный статус
    /// </summary>
    public async Task<IReadOnlyCollection<FinanceOperationIdentification>> GetUnfinishedOperations(KeySetPagination<FinanceOperationIdentification>? pagination)
    {
        var query = Ctx.Set<FinanceOperation>()
                .Where(fo => fo.State != FinanceOperationState.Declined && fo.State != FinanceOperationState.Approved)
                .Where(fo => fo.OperationType == FinanceOperationType.Online)
                .Apply(pagination, x => x.CommentId)
                .Select(fo => new { fo.ProjectId, fo.ClaimId, fo.CommentId });

        var result = await query.ToArrayAsync();
        return [.. result.Select(r => new FinanceOperationIdentification(r.ProjectId, r.ClaimId, r.CommentId))];
    }
}
