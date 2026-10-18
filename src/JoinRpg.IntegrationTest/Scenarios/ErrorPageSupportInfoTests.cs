using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Страница ошибки показывает данные для техподдержки, в том числе имя пода — по нему ищутся
/// логи конкретной реплики. Страница перезапускается тем же методом, что и упавший запрос,
/// поэтому работать она должна при любом методе, а не только при GET и POST.
/// </summary>
[Collection("XApi")]
public class ErrorPageSupportInfoTests(XApiMasterFixture fixture)
{
    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    [InlineData("PUT")]
    public async Task NotFound_ShowsSupportInfoWithPodName(string method)
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.SendAsync(new HttpRequestMessage(new HttpMethod(method), "/this-page-does-not-exist"));

        // Кроме GET, на неизвестный адрес бывает и 405 — от маршрутов, принимающих только GET.
        response.StatusCode.ShouldBeOneOf(HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed);
        var html = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());
        html.ShouldContain("Данные для техподдержки");
        html.ShouldContain(Environment.MachineName);
    }
}
