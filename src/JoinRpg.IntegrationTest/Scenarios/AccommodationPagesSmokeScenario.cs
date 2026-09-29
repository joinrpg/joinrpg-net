using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Web.Models.Accommodation;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страховочная сетка на HTTP-уровне для страницы поселения (<c>/{projectId}/rooms</c>).
/// </summary>
/// <remarks>
/// Подсказки на странице рисует Blazor-компонент <c>Tooltip</c>, который в MVC-разметке
/// подключается тег-хелпером <c>&lt;component&gt;</c>. Такое подключение не проверяется
/// компилятором: опечатка в имени параметра или несовместимый тип значения ломают страницу
/// только в рантайме. Поэтому тест смотрит не код ответа (это делает
/// <see cref="AllGetPagesSmokeScenario"/>), а разметку: подсказки отрисованы контейнером
/// join-tooltip и ни одна из них не осталась нативным атрибутом title.
/// </remarks>
public class AccommodationPagesSmokeScenario(SmokeProjectFixture fixture) : IClassFixture<SmokeProjectFixture>
{
    [Fact]
    public async Task RoomsPage_RendersHintsAsTooltipComponent()
    {
        if (!fixture.Values.TryGetValue("projectId", out var projectId))
        {
            throw new InvalidOperationException("Сид смоука не зарегистрировал projectId");
        }

        var response = await fixture.MasterClient.GetAsync($"{projectId.RouteValue}/rooms");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var document = await response.AsHtmlDocument();
        var table = document.DocumentNode.SelectSingleNode("//table")
            ?? throw new InvalidOperationException("На странице поселения нет таблицы типов размещения");

        table.SelectNodes(".//span[contains(@class, 'join-tooltip')]")
            .ShouldNotBeNull("Подсказки на странице поселения не отрисованы компонентом Tooltip");

        var text = WebUtility.HtmlDecode(table.InnerText);
        text.ShouldContain(RoomTypeListItemViewModel.CapacityTooltip);
        text.ShouldContain(RoomTypeListItemViewModel.TotalUnsettledTooltip);

        // Нативные подсказки браузера выглядят иначе, чем join-tooltip, поэтому на странице
        // не должно остаться ни одной: смесь двух видов подсказок — это баг разметки.
        table.SelectNodes(".//*[@title]")
            .ShouldBeNull("На странице поселения остались нативные подсказки title=");
    }
}
