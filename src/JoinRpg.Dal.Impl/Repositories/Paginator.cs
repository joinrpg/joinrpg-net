using JoinRpg.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

public static class Paginator
{
    public static IQueryable<T> ApplyPaginationEf<T>(this IQueryable<T> query, KeySetPagination? pagination, Expression<Func<T, int>> keySelector)
    {
        pagination ??= new KeySetPagination();
        if (pagination.Direction == KeySetDirection.Descending)
        {
            if (pagination.From is int before)
            {
                query = query.Where(entity => keySelector.Invoke(entity) < before);
            }
            return query.OrderByDescending(keySelector).Take(pagination.PageSize);
        }
        var id = pagination.From ?? 0;
        return
            query
            .OrderBy(keySelector)
            .Where(entity => keySelector.Invoke(entity) > id)
            .Take(pagination.PageSize);
    }
}
