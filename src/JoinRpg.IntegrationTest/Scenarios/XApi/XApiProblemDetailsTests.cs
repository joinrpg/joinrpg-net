using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Ошибки внешнего XApi отдаются ProblemDetails (машиночитаемый JSON), а не пустым телом
/// или HTML-страницей /error. Коды ответов по сравнению с прежним поведением не меняются.
/// </summary>
[Collection("XApi")]
public class XApiProblemDetailsTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task MasterIsNotSiteAdmin_Returns403ProblemDetails()
    {
        var response = await fixture.MasterClient.GetUserInfoMessageAsync(fixture.MasterUserId.Value);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Недостаточно прав для этого действия");
    }

    [Fact]
    public async Task WithoutToken_Returns401ProblemDetails()
    {
        var response = await fixture.AnonymousXApiClient.GetUserInfoMessageAsync(1);

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        response.Headers.WwwAuthenticate.ShouldNotBeEmpty();
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Токен доступа отсутствует, невалиден или истёк");
    }

    [Fact]
    public async Task PlayerIsNotMaster_Returns403ProblemDetails()
    {
        const string password = "Password123!";
        var (_, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services, password: password);
        var playerClient = await XApiClient.CreateXApiClient(fixture.Factory.CreateClient(), playerEmail, password);

        var response = await playerClient.GetFieldsMetadataMessageAsync(fixture.ProjectId.Value);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Недостаточно прав для этого действия");
    }

    [Fact]
    public async Task ProjectNotFound_Returns404ProblemDetails()
    {
        var response = await fixture.MasterClient.GetFieldsMetadataMessageAsync(int.MaxValue);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Проект не найден");
    }

    [Fact]
    public async Task UnknownEndpoint_Returns404ProblemDetails()
    {
        var response = await fixture.MasterClient.GetRawAsync($"/x-game-api/{fixture.ProjectId.Value}/no-such-endpoint");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound, await response.Content.ReadAsStringAsync());
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        (await response.Content.ReadAsStringAsync()).ShouldContain("Не найдено");
    }
}
