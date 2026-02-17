using System.Linq.Expressions;
using JoinRpg.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.CommonEfCore;

public static class Paginator
{
    public static IQueryable<T> ApplyPaginationEfCore<T>(this IQueryable<T> query, KeySetPagination? pagination, Expression<Func<T, int>> keySelector)
    {
        pagination ??= new KeySetPagination();
        var id = pagination.From ?? 0;
        return
            query
            .OrderBy(keySelector)
            .Where(entity => keySelector.Invoke(entity) > id)
            .Take(pagination.PageSize);
    }
}
