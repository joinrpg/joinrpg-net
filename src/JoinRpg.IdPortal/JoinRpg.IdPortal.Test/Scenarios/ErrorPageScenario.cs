using System.Net;

namespace JoinRpg.IdPortal.Test.Scenarios;

/// <summary>
/// Страница ошибки IdPortal: на неизвестный адрес — страница с данными для техподдержки,
/// а эндпоинты OAuth отвечают по протоколу, без HTML.
/// </summary>
[Collection("IdPortal")]
public class ErrorPageScenario(IdPortalApplicationFactory factory)
{
    [Fact]
    public async Task NotFound_ShowsNotFoundPageWithPodName()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/this-page-does-not-exist");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.ShouldContain("Страница не найдена");
        html.ShouldContain("Данные для техподдержки");
        html.ShouldContain(Environment.MachineName);
    }

    // Без UseErrorPageReExecuteAsGet страница ошибки получала бы метод упавшего запроса: на POST
    // Blazor ищет отправленную форму и падает, а DELETE и PUT не принимает вовсе.
    [Theory]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    public async Task NotGetRequest_StillShowsSupportInfo(string method)
    {
        var client = factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/this-page-does-not-exist"));

        // На неизвестный адрес бывает и 405 — от маршрутов, принимающих только GET.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.ShouldContain("Данные для техподдержки");
        html.ShouldContain(Environment.MachineName);
    }

    [Fact]
    public async Task UserInfoWithoutToken_IsProtocolChallenge_NotHtmlPage()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/connect/user_info");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldNotBeEmpty();
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/html");
    }

    [Fact]
    public async Task UnknownWellKnown_IsNotHtmlPage()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/.well-known/no-such-document");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldNotBe("text/html");
    }
}
