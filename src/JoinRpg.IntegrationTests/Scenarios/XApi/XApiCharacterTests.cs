using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCharacterTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var response = await fixture.Factory.CreateClient()
            .GetAsync($"x-game-api/{fixture.ProjectId}/characters");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetEmptyCharacterList()
    {
        var characters = await fixture.AuthorizedClient.GetCharactersAsync(fixture.ProjectId);

        characters.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetNonexistentCharacter_ReturnsError()
    {
        var exception = await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AuthorizedClient.GetCharacterAsync(fixture.ProjectId, characterId: 999999));

        ((int)exception.StatusCode!).ShouldBeGreaterThanOrEqualTo(400);
    }
}
