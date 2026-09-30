using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страницы расселения не должны догружать жильцов по одному (#5070).
/// </summary>
/// <remarks>
/// Почему это не ловилось смоуком раньше. Оба маршрута он не мерил:
/// <list type="bullet">
/// <item><description>
/// на <c>rooms/{roomTypeId}/details</c> смоук ходил, но в сиде у типа поселения не было ни одного
/// жильца — цикл по <c>Desirous</c> не делал ни одной итерации, и маршрут честно измерялся в ноль
/// ленивых загрузок. На проде тот же код давал 296 догрузок за одно окно — по персонажу, игроку и
/// денежным операциям на каждого из 79 жильцов. Теперь жильцы в сиде есть
/// (<see cref="SmokeProjectFixture"/>), и маршрут меряется по-настоящему;
/// </description></item>
/// <item><description>
/// <c>rooms/report</c> смоук вообще пропускал: без параметра <c>export</c> страница отдаёт 404,
/// отчёт существует только как выгрузка. Его и покрывает этот сценарий.
/// </description></item>
/// </list>
/// Сами ленивые загрузки считает <c>LazyLoadAssertingHandler</c> на клиенте фабрики и сверяет с
/// <c>lazy-loads-baseline.json</c> (см. docs/lazy-loads-baseline.md). Долг обоих маршрутов там
/// теперь зафиксирован, и задача — свести его к нулю (#5037), после чего строчки из снапшота уйдут.
/// Поэтому тут важно, чтобы страница реально вывела жильцов: на пустом списке N+1 не случится, и
/// тест снова станет пустым.
/// </remarks>
[Collection(SmokeCollection.Name)]
public class AccommodationPagesLazyLoadsScenario(SmokeProjectFixture fixture)
{
    [Fact]
    public async Task RoomTypeDetails_ListsInhabitants()
    {
        var url = $"{fixture.ProjectId.Value}/rooms/{fixture.RoomTypeId}/details";

        var response = await fixture.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {url} не открылась");

        var document = await response.AsHtmlDocument();
        var text = WebUtility.HtmlDecode(
            document.DocumentNode.SelectSingleNode("//body")?.InnerText
            ?? throw new InvalidOperationException("Страница не содержит body"));

        // Сами комнаты тоже должны отрисоваться: строка комнаты несёт атрибут roomId, по нему
        // её находит rooms.js (имена атрибутов HtmlAgilityPack приводит к нижнему регистру).
        // Пустая таблица означала бы, что страница собралась мимо плана поселения (ADR018).
        var roomRows = document.DocumentNode.SelectNodes("//tr[@roomid]");
        roomRows.ShouldNotBeNull("На странице нет ни одной комнаты");

        // Первая ячейка строки — название комнаты.
        var shownRooms = roomRows
            .Select(row => WebUtility.HtmlDecode(row.SelectSingleNode("td").InnerText).Trim())
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();
        shownRooms.ShouldBe(
            [.. fixture.RoomNames.OrderBy(name => name, StringComparer.Ordinal)],
            "На странице показаны не те комнаты, что созданы в сиде");

        // Страница показывает всех, у кого есть заявка на проживание, вне зависимости от её статуса.
        foreach (var resident in fixture.Residents)
        {
            text.ShouldContain(
                resident.DisplayName,
                customMessage:
                    $"Жилец {resident.DisplayName} не попал на страницу — тест перестал проверять N+1");
        }
    }

    [Fact]
    public async Task AccommodationReport_ExportsInhabitantsWithPhones()
    {
        var url = $"{fixture.ProjectId.Value}/rooms/report/?export=csv";

        var response = await fixture.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Выгрузка {url} не отдалась");

        var csv = await response.Content.ReadAsStringAsync();

        foreach (var resident in fixture.Residents)
        {
            if (!resident.ClaimIsActive)
            {
                // Отложенная заявка в отчёт не попадает, см. SmokeResident.
                csv.ShouldNotContain(resident.DisplayName);
                continue;
            }

            csv.ShouldContain(
                resident.DisplayName,
                customMessage: $"В отчёте нет жильца {resident.DisplayName}");

            // Телефон приезжает из UserExtra — именно эта колонка и давала догрузку на строку.
            csv.ShouldContain(
                resident.Phone,
                customMessage: $"В отчёте нет телефона жильца {resident.DisplayName}");
        }
    }
}
