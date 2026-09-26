using System.Collections.Concurrent;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Замеры ленивых загрузок по запросам, сделанным через тестовый HTTP-клиент.
/// </summary>
/// <remarks>
/// Клиент и сервер живут в одном процессе, поэтому замер не надо передавать через заголовок ответа:
/// <see cref="LazyLoadObservingMiddleware"/> кладёт его сюда, а <see cref="LazyLoadAssertingHandler"/>
/// забирает по тому же идентификатору запроса.
///
/// Заголовком ответа это сделать и нельзя: заголовки уходят до рендеринга Razor-вью, а ленивые
/// загрузки во вью — ровно то, что мы ищем.
/// </remarks>
public sealed class LazyLoadObservations
{
    /// <summary>Заголовок запроса, которым клиент помечает вызов, чтобы узнать свой замер.</summary>
    public const string RequestIdHeader = "X-Test-Request-Id";

    private readonly ConcurrentDictionary<string, LazyLoadObservation> observations = new(StringComparer.Ordinal);

    public void Record(string requestId, LazyLoadObservation observation) => observations[requestId] = observation;

    public bool TryTake(string requestId, out LazyLoadObservation observation)
        => observations.TryRemove(requestId, out observation);
}

/// <summary>Сколько ленивых загрузок стоил один запрос к маршруту <paramref name="Route"/>.</summary>
/// <param name="Route">Ключ маршрута вида <c>GET /{projectId}/plots/edit</c>.</param>
/// <param name="LazyLoads">Число ленивых загрузок за запрос.</param>
public readonly record struct LazyLoadObservation(string Route, int LazyLoads);
