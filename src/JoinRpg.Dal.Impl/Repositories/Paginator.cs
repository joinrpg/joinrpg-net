using System.Linq.Expressions;
using JoinRpg.Data.Interfaces;
using JoinRpg.PrimitiveTypes;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

public static class Paginator
{
    public static IQueryable<T> Apply<T, TId>(this IQueryable<T> query, KeySetPagination<TId>? pagination, Expression<Func<T, int>> keySelector)
        where TId : class, IProjectEntityId
    {
        pagination ??= new KeySetPagination<TId>();
        var id = pagination.From?.Id ?? 0;
        return
            query
            .OrderBy(keySelector)
            .Where(entity => keySelector.Invoke(entity) > id)
            .Take(pagination.PageSize);
    }
}
