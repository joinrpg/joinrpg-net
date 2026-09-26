using JoinRpg.Dal.Impl;

namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Запоминает, сколько ленивых загрузок стоил запрос, и под каким маршрутом его записать.
/// </summary>
/// <remarks>
/// Счётчик заводится на время обработки запроса, поэтому параллельные тесты друг другу не мешают —
/// в отличие от метрики <c>joinrpg.mydbcontext.lazy_loads</c>, глобальной на процесс.
///
/// Маршрут известен только после того, как отработал пайплайн: до этого роутинг ещё не выбрал endpoint.
/// </remarks>
internal sealed class LazyLoadObservingMiddleware(RequestDelegate next, LazyLoadObservations observations)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var requestId = context.Request.Headers[LazyLoadObservations.RequestIdHeader].ToString();

        if (string.IsNullOrEmpty(requestId))
        {
            // Запрос не от тестового клиента (например, внутренний) — замер никому не нужен.
            await next(context);
            return;
        }

        using var counter = LazyLoadCounter.BeginScope();

        await next(context);

        observations.Record(requestId, new LazyLoadObservation(GetRouteKey(context), counter.Count));
    }

    /// <summary>
    /// Ключ маршрута, а не URL: в URL сидят идентификаторы проектов и сущностей,
    /// они меняются от прогона к прогону.
    /// </summary>
    private static string GetRouteKey(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        var pattern = (endpoint as RouteEndpoint)?.RoutePattern.RawText
            ?? endpoint?.DisplayName
            ?? context.Request.Path.Value
            ?? "/";
        return $"{context.Request.Method} /{pattern.TrimStart('/')}";
    }
}
