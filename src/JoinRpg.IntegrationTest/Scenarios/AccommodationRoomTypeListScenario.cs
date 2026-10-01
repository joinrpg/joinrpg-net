using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница списка типов проживания <c>{projectId}/rooms/</c> должна показывать реальную занятость.
/// </summary>
/// <remarks>
/// Смоук по страницам (<see cref="AllGetPagesSmokeScenario"/>) проверяет только код ответа, поэтому
/// страница считалась покрытой, пока отдаёт 200 — а её содержимое это арифметика поверх плана
/// поселения (свободно/занято, целые и частично занятые комнаты), и после переезда на
/// <c>RoomCategoryPlan</c> (ADR018) она могла бы молча считать не то.
///
/// Цифры формул размечены tooltip-ами (<see cref="RoomTypeListItemViewModel"/>), по ним же они и
/// находятся: разметка меняется, а подписи — часть того, что видит пользователь.
/// </remarks>
[Collection(SmokeCollection.Name)]
public class AccommodationRoomTypeListScenario(SmokeProjectFixture fixture)
{
    [Fact]
    public async Task RoomTypeList_ShowsOccupancyCounters()
    {
        var url = $"{fixture.ProjectId.Value}/rooms/";

        var response = await fixture.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {url} не открылась");

        var document = await response.AsHtmlDocument();

        // Все размеченные подписями цифры страницы. Искать их сразу через XPath по значению
        // атрибута нельзя: Razor кодирует кириллицу в атрибутах числовыми сущностями
        // (&#x44D;…), поэтому подписи сначала надо раскодировать.
        var counterNodes = document.DocumentNode.SelectNodes("//span[@title]")
            ?? throw new InvalidOperationException($"На странице {url} нет ни одной размеченной цифры");
        var counters = counterNodes
            .GroupBy(node => WebUtility.HtmlDecode(node.GetAttributeValue("title", "")))
            .ToDictionary(
                group => group.Key,
                group => group.Select(node => WebUtility.HtmlDecode(node.InnerText).Trim()).ToList());

        var placed = fixture.Residents.Count(resident => resident.IsPlaced);
        var totalCapacity = fixture.RoomCapacity * fixture.RoomNames.Count;

        // В сиде один тип проживания, поэтому итог по строке типа и итог по странице совпадают,
        // а сама формула не должна зависеть от того, как жильцы разложены по комнатам.
        ReadCounter(RoomTypeListItemViewModel.TotalOccupiedTooltip).ShouldBe(
            placed,
            "Счётчик занятых мест не совпал с числом расселённых жильцов сида");
        ReadCounter(RoomTypeListItemViewModel.TotalFreeTooltip).ShouldBe(
            totalCapacity - placed,
            "Счётчик свободных мест не совпал с вместимостью минус расселённые");
        ReadCounter(RoomTypeListItemViewModel.FullyFreeRoomsTooltip).ShouldBe(
            fixture.RoomNames.Count - 1,
            "Одна комната сида занята частично, остальные должны считаться полностью свободными");

        // Строка «Итого» считается отдельно от строк типов — и должна давать то же самое.
        // В строке типа те же числа выводятся формулой («Занято: 4 ✕ 0 + 2 = 2»), поэтому
        // «Занято: 2» встречается только в итоге.
        var pageText = WebUtility.HtmlDecode(
            document.DocumentNode.SelectSingleNode("//body")?.InnerText
            ?? throw new InvalidOperationException("Страница не содержит body"));
        pageText.ShouldContain($"Занято: {placed}", customMessage: "Итог по занятым местам не тот");
        pageText.ShouldContain(
            $"Свободно: {totalCapacity - placed}",
            customMessage: "Итог по свободным местам не тот");

        int ReadCounter(string tooltip)
        {
            var values = counters.GetValueOrDefault(tooltip)
                ?? throw new InvalidOperationException(
                    $"На странице {url} нет цифры с подписью «{tooltip}»");

            // Тип проживания в сиде один: если счётчиков стало больше, сравнивать «тот самый»
            // с ожидаемым числом уже нельзя, и тест должен об этом сказать, а не взять первый.
            values.Count.ShouldBe(
                1,
                $"Цифр с подписью «{tooltip}» на странице {values.Count}, а тип проживания в сиде один");

            return int.Parse(values[0]);
        }
    }
}
