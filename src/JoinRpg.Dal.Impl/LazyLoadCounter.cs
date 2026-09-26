namespace JoinRpg.Dal.Impl;

/// <summary>
/// Считает ленивые загрузки EF6 внутри своего блока — например, за один HTTP-запрос.
/// </summary>
/// <remarks>
/// Счётчик амбиентный (<see cref="AsyncLocal{T}"/>), а не из DI, по двум причинам.
/// Во-первых, <see cref="MyDbContext"/> транзиентен, за запрос их создаётся несколько, и поле
/// в контексте счёт бы размазало. Во-вторых, scoped-регистрация сделала бы контекст
/// scoped-зависимым, и валидация скоупов в IdPortal отвергла бы синглтоны, которые его потребляют
/// (например <c>IVirtualUsersService</c>).
///
/// Инкрементит <see cref="EF6LoggerToMSExtLogging"/> — там же, где пишет метрику
/// <c>joinrpg.mydbcontext.lazy_loads</c>. В проде счётчик никто не заводит и накладных расходов нет:
/// вне блока <see cref="BeginScope"/> там просто null.
///
/// Нужен интеграционным тестам (см. #4914). Параллельные запросы друг другу не мешают: у каждого
/// свой блок в своей ветке <see cref="System.Threading.ExecutionContext"/>. Фоновая работа,
/// запущенная изнутри блока, в счёт попадёт — по той же причине.
/// </remarks>
public sealed class LazyLoadCounter : IDisposable
{
    private static readonly AsyncLocal<LazyLoadCounter?> current = new();

    private readonly LazyLoadCounter? previous;
    private int count;

    private LazyLoadCounter()
    {
        previous = current.Value;
        current.Value = this;
    }

    /// <summary>Начинает считать ленивые загрузки до конца блока.</summary>
    public static LazyLoadCounter BeginScope() => new();

    internal static LazyLoadCounter? Current => current.Value;

    /// <summary>Сколько ленивых загрузок насчитано к текущему моменту.</summary>
    public int Count => Volatile.Read(ref count);

    internal void Increment() => _ = Interlocked.Increment(ref count);

    public void Dispose() => current.Value = previous;
}
