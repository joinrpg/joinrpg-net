using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Содержимое заглавной страницы конверта на странице печати конвертов C5.
/// </summary>
/// <remarks>
/// Смоук по страницам проверяет только код ответа, а конверт собирается из трёх источников —
/// агрегата персонажа, телефонов игроков и планов поселения. Если источник подключён неверно
/// (телефон не того игрока, комната не та, блок поселения исчез), страница продолжит отдавать 200,
/// и расхождение дойдёт до распечатки перед игрой. Поэтому здесь проверяются значения, а не разметка.
/// </remarks>
[Collection(SmokeCollection.Name)]
public class PrintEnvelopeContentScenario(SmokeProjectFixture fixture)
{
    [Fact]
    public async Task EnvelopesC5_PrintsPlayerContactsAndAccommodationOfApprovedClaim()
    {
        var url = $"{fixture.ProjectId.Value}/print/envelopesc5";

        var response = await fixture.MasterClient.GetAsync(url);
        response.StatusCode.ShouldBe(HttpStatusCode.OK, $"Страница {url} не открылась");

        var document = await response.AsHtmlDocument();
        var body = document.DocumentNode.SelectSingleNode("//body")
            ?? throw new InvalidOperationException($"Страница {url} не содержит body");

        // Кириллица в разметке приезжает числовыми HTML-сущностями, InnerText их не раскрывает.
        var text = WebUtility.HtmlDecode(body.InnerText);

        // Игрок утверждённой заявки сида: он же расселён в комнату (см. SmokeProjectFixture).
        var resident = fixture.Residents[0];

        text.ShouldContain(resident.DisplayName);
        text.ShouldContain(resident.Phone, customMessage: "Телефон игрока печатается на конверте");
        text.ShouldContain("Поселение");
        text.ShouldContain(
            fixture.RoomNames[0],
            customMessage: "На конверте должна быть комната, в которую расселена заявка");
    }
}
