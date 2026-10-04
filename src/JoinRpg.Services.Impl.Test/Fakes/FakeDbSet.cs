using System.Collections;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Linq.Expressions;

namespace JoinRpg.Services.Impl.Test.Fakes;

/// <summary>
/// <see cref="DbSet{TEntity}"/> поверх обычной коллекции мока: чтение — LINQ по памяти,
/// <c>Add</c>/<c>Remove</c> — в ту же коллекцию.
/// </summary>
/// <remarks>
/// Нужен там, где проверяемый сервис ещё не переехал на write-репозитории (ADR009/ADR014) и ходит
/// в <c>UnitOfWork.GetDbSet</c> напрямую. Поэтому
/// набор подключается к <see cref="FakeUnitOfWork"/> поштучно и явно
/// (<see cref="FakeUnitOfWork.UseDbSet{T}"/>): у сервисов, которые в DbSet лазить не должны,
/// <c>GetDbSet</c> обязан по-прежнему падать.
/// <para>
/// EF6 умеет <c>ToArrayAsync</c>/<c>FirstOrDefaultAsync</c> только над провайдером, который
/// реализует <see cref="IDbAsyncQueryProvider"/>, поэтому обычного <c>List.AsQueryable()</c>
/// недостаточно — асинхронность здесь синхронная (см. <see cref="FakeDbAsyncQueryProvider{T}"/>).
/// <c>Include</c> же на «не-EF» источнике EF6 просто возвращает его как есть: жадная загрузка в
/// памяти не нужна, граф мока и так связан навигациями.
/// </para>
/// </remarks>
internal sealed class FakeDbSet<T>(ICollection<T> data)
    : DbSet<T>, IQueryable<T>, IEnumerable<T>, IDbAsyncEnumerable<T>
    where T : class
{
    private IQueryable<T> Query => data.AsQueryable();

    public override T Add(T entity)
    {
        data.Add(entity);
        return entity;
    }

    public override T Remove(T entity)
    {
        _ = data.Remove(entity);
        return entity;
    }

    public override IEnumerable<T> AddRange(IEnumerable<T> entities)
    {
        var added = entities.ToList();
        foreach (var entity in added)
        {
            data.Add(entity);
        }

        return added;
    }

    public override IEnumerable<T> RemoveRange(IEnumerable<T> entities)
    {
        var removed = entities.ToList();
        foreach (var entity in removed)
        {
            _ = data.Remove(entity);
        }

        return removed;
    }

    Type IQueryable.ElementType => typeof(T);

    Expression IQueryable.Expression => Query.Expression;

    IQueryProvider IQueryable.Provider => new FakeDbAsyncQueryProvider<T>(Query.Provider);

    public IEnumerator<T> GetEnumerator() => data.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => data.GetEnumerator();

    IDbAsyncEnumerator<T> IDbAsyncEnumerable<T>.GetAsyncEnumerator()
        => new FakeDbAsyncEnumerator<T>(data.GetEnumerator());

    IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator()
        => new FakeDbAsyncEnumerator<T>(data.GetEnumerator());
}

/// <summary>
/// Провайдер запросов, который выполняет всё синхронно, но виден EF6 как асинхронный.
/// </summary>
internal sealed class FakeDbAsyncQueryProvider<T>(IQueryProvider inner) : IDbAsyncQueryProvider
{
    public IQueryable CreateQuery(Expression expression) => new FakeDbAsyncEnumerable<T>(expression);

    public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        => new FakeDbAsyncEnumerable<TElement>(expression);

    // Интерфейс EF6 не размечен nullable-аннотациями и обещает object: скалярный запрос по
    // пустой выборке всё же вернёт null, и это нормально — так же ведёт себя настоящий провайдер.
    public object Execute(Expression expression) => inner.Execute(expression)!;

    public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

    public Task<object> ExecuteAsync(Expression expression, CancellationToken cancellationToken)
        => Task.FromResult(Execute(expression));

    public Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        => Task.FromResult(Execute<TResult>(expression));
}

/// <summary>
/// Результат промежуточного запроса (<c>Where</c>, <c>Select</c>…): такой же «асинхронный»
/// источник, чтобы на нём снова работали <c>ToArrayAsync</c> и подобные.
/// </summary>
internal sealed class FakeDbAsyncEnumerable<T> : EnumerableQuery<T>, IDbAsyncEnumerable<T>, IQueryable<T>
{
    public FakeDbAsyncEnumerable(Expression expression) : base(expression) { }

    public IDbAsyncEnumerator<T> GetAsyncEnumerator()
        => new FakeDbAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

    IDbAsyncEnumerator IDbAsyncEnumerable.GetAsyncEnumerator() => GetAsyncEnumerator();

    IQueryProvider IQueryable.Provider => new FakeDbAsyncQueryProvider<T>(this);
}

/// <summary>Перечислитель поверх обычного: каждый шаг — уже готовый результат.</summary>
internal sealed class FakeDbAsyncEnumerator<T>(IEnumerator<T> inner) : IDbAsyncEnumerator<T>
{
    public T Current => inner.Current;

    object? IDbAsyncEnumerator.Current => Current;

    public Task<bool> MoveNextAsync(CancellationToken cancellationToken)
        => Task.FromResult(inner.MoveNext());

    public void Dispose() => inner.Dispose();
}
