namespace JoinRpg.IntegrationTest.TestInfrastructure;

/// <summary>
/// Помечает каждый запрос тестового клиента и сверяет его замер ленивых загрузок со снапшотом.
/// </summary>
/// <remarks>
/// Подключается в <see cref="JoinApplicationFactory"/> сразу на все тестовые клиенты, поэтому
/// любой тест, который дёргает страницу, начинает проверяться без единой правки в самом тесте.
///
/// Ставится последним в цепочке (ближе всего к серверу): иначе <c>RedirectHandler</c> проглотил бы
/// промежуточные ответы, и проверялись бы только конечные.
/// </remarks>
internal sealed class LazyLoadAssertingHandler(LazyLoadObservations observations, LazyLoadBaseline baseline)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var requestId = Guid.NewGuid().ToString("N");
        request.Headers.Add(LazyLoadObservations.RequestIdHeader, requestId);

        var response = await base.SendAsync(request, cancellationToken);

        // TestServer отдаёт ответ, как только пошли заголовки, а ленивые загрузки бывают и при
        // рендеринге вью. Дочитываем тело, чтобы middleware гарантированно успел записать замер.
        await response.Content.LoadIntoBufferAsync(cancellationToken);

        if (observations.TryTake(requestId, out var observation))
        {
            baseline.Check(observation);
        }

        return response;
    }
}
