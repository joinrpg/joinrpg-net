using System.Data.Common;
using System.Data.Entity.Infrastructure.Interception;

namespace JoinRpg.IntegrationTests.TestInfrastructure;

/// <summary>
/// Считает ленивые загрузки таргетов вводных (таблицы связи <c>PlotElementCharacters</c>
/// и <c>PlotElementCharacterGroups</c>), пока объект жив.
/// </summary>
/// <remarks>
/// Почему интерцептор, а не метрика <c>joinrpg.mydbcontext.lazy_loads</c>: единственный тег этой метрики —
/// <c>operation</c>, и он всегда равен <c>Microsoft.AspNetCore.Hosting.HttpRequestIn</c>, без маршрута.
/// Отобрать по нему замеры конкретной страницы невозможно, так что <c>MeterListener</c> дал бы
/// либо ноль, либо сумму по всем тестам сразу. Постоянное решение — per-request счётчик
/// внутри <c>MyDbContext</c>, см. #4914.
///
/// Ленивая загрузка узнаётся по той же подписи, что использует <c>EF6LoggerToMSExtLogging</c>:
/// единственный параметр запроса с именем <c>EntityKeyValue1</c>. Благодаря этому запросы
/// с <c>Include</c> (их делает сам фикс) в счёт не попадают — у них параметры другие.
///
/// <c>DbInterception</c> глобален на процесс, поэтому сценарий, который этим пользуется,
/// не должен идти параллельно с другими тестами, трогающими сюжеты.
/// </remarks>
public sealed class LazyTargetLoadCounter : IDbCommandInterceptor, IDisposable
{
    private const string LazyLoadParameterName = "EntityKeyValue1";

    // Префикс покрывает обе таблицы связи: PlotElementCharacters и PlotElementCharacterGroups.
    private const string TargetTablePrefix = "PlotElementCharacter";

    private int count;

    private LazyTargetLoadCounter()
    {
    }

    /// <summary>Начинает считать ленивые загрузки таргетов вводных.</summary>
    public static LazyTargetLoadCounter Start()
    {
        var counter = new LazyTargetLoadCounter();
        DbInterception.Add(counter);
        return counter;
    }

    /// <summary>Сколько ленивых загрузок таргетов насчитано к текущему моменту.</summary>
    public int Count => Volatile.Read(ref count);

    public void ReaderExecuting(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext)
    {
        if (command.Parameters.Count == 1
            && command.Parameters[0].ParameterName == LazyLoadParameterName
            && command.CommandText.Contains(TargetTablePrefix, StringComparison.Ordinal))
        {
            _ = Interlocked.Increment(ref count);
        }
    }

    public void Dispose() => DbInterception.Remove(this);

    public void ReaderExecuted(DbCommand command, DbCommandInterceptionContext<DbDataReader> interceptionContext) { }
    public void NonQueryExecuting(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) { }
    public void NonQueryExecuted(DbCommand command, DbCommandInterceptionContext<int> interceptionContext) { }
    public void ScalarExecuting(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) { }
    public void ScalarExecuted(DbCommand command, DbCommandInterceptionContext<object> interceptionContext) { }
}
