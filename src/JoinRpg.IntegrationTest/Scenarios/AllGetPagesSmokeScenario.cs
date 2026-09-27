using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Обходит под мастером все GET-эндпоинты приложения, до которых можно дойти с сид-проекта (#4956).
/// </summary>
/// <remarks>
/// Зачем generic-смоук, а не сценарий на страницу: механизм из #4914 меряет ленивые загрузки
/// только там, куда тесты реально сходили, поэтому снапшот знал лишь про горстку маршрутов, а
/// долг по #4670 жил на остальных — его находили в логах прода, а не в тестах. Обход по
/// <see cref="EndpointDataSource"/> закрывает это разом и, в отличие от списка URL в тесте,
/// не отстаёт от приложения: новая страница попадает под проверку сама.
///
/// Проверяется только код ответа: осмысленность содержимого — дело точечных сценариев
/// (<see cref="PlotPagesSmokeScenario"/>, <see cref="PrintPagesSmokeScenario"/>), у которых на
/// каждую страницу есть что утверждать. Главная же ценность прогона — побочная: на каждом запросе
/// срабатывает <see cref="LazyLoadAssertingHandler"/>, так что снапшот ленивых загрузок наполняется
/// по всему приложению, а не по 30 случайным маршрутам.
///
/// Все кейсы идут одним тестом, а не Theory: во-первых, список эндпоинтов известен только после
/// поднятия хоста, а MemberData вычисляется раньше; во-вторых, отчёт из одного сообщения со всеми
/// упавшими маршрутами полезнее, чем первый упавший кейс из двухсот.
/// </remarks>
public class AllGetPagesSmokeScenario(SmokeProjectFixture fixture) : IClassFixture<SmokeProjectFixture>
{
    [Fact]
    public async Task EveryReachableGetEndpoint_AnswersExpectedStatusUnderMaster()
    {
        var endpoints = SmokeEndpointDiscovery.DiscoverGetEndpoints(fixture.Factory.Services);
        endpoints.ShouldNotBeEmpty("Не нашлось ни одного GET-эндпоинта — сломалось обнаружение маршрутов");

        var failures = new List<string>();
        var visited = 0;

        foreach (var endpoint in endpoints)
        {
            if (SmokeExpectations.Skipped.ContainsKey(endpoint.RouteTemplate))
            {
                continue;
            }

            var built = SmokeUrlBuilder.Build(endpoint, fixture.Values);
            if (built.Url is null)
            {
                failures.Add(
                    $"{endpoint}: нечего подставить в параметры {string.Join(", ", built.MissingParameters)}. "
                    + "Досейте значение в SmokeProjectFixture или запишите маршрут в SmokeExpectations.Skipped с причиной");
                continue;
            }

            var expected = SmokeExpectations.ExpectedStatusFor(endpoint);
            try
            {
                var response = await fixture.MasterClient.GetAsync(built.Url);
                visited++;

                if (response.StatusCode != expected)
                {
                    failures.Add($"{endpoint}: GET /{built.Url} ответил {(int)response.StatusCode}, ожидался {(int)expected}");
                }
            }
            catch (LazyLoadBaselineException e)
            {
                // Ловим, а не роняем тест сразу: иначе первый же маршрут с выросшим N+1 скрыл бы
                // результат по всем остальным, а смысл прогона — увидеть картину целиком.
                failures.Add($"{endpoint}: GET /{built.Url} — {e.Message}");
            }
            catch (Exception e)
            {
                failures.Add($"{endpoint}: GET /{built.Url} упал с {e.GetType().Name}: {e.Message}");
            }
        }

        visited.ShouldBeGreaterThan(
            endpoints.Count / 2,
            "Смоук дошёл меньше чем до половины эндпоинтов — похоже, сид развалился");

        failures.ShouldBeEmpty(
            $"Смоук по страницам проекта нашёл проблемы ({failures.Count} из {endpoints.Count} эндпоинтов):"
            + Environment.NewLine
            + string.Join(Environment.NewLine, failures));
    }

    /// <summary>
    /// Ожидания не должны ссылаться на маршруты, которых в приложении уже нет.
    /// </summary>
    /// <remarks>
    /// Иначе списки превращаются в свалку: маршрут переименовали, страница выпала из-под смоука,
    /// а запись с причиной осталась и выглядит как покрытие.
    /// </remarks>
    [Fact]
    public void Expectations_DoNotReferenceUnknownRoutes()
    {
        var known = SmokeEndpointDiscovery.DiscoverGetEndpoints(fixture.Factory.Services)
            .Select(e => e.RouteTemplate)
            .ToHashSet(StringComparer.Ordinal);

        var stale = SmokeExpectations.Skipped.Keys
            .Concat(SmokeExpectations.ExpectedStatus.Keys)
            .Where(route => !known.Contains(route))
            .Order(StringComparer.Ordinal)
            .ToList();

        stale.ShouldBeEmpty(
            "В SmokeExpectations остались записи про маршруты, которых в приложении нет — удалите их: "
            + string.Join(", ", stale));
    }
}
