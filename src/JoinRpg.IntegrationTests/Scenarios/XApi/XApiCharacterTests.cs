using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCharacterTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var client = new XApiClient(fixture.Factory.CreateClient());
        var response = await client.GetCharactersRawAsync(fixture.ProjectId);
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task MasterCanGetEmptyCharacterList()
    {
        var characters = await fixture.AuthorizedClient.GetCharactersAsync(fixture.ProjectId);
        characters.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetNonexistentCharacter_ReturnsError()
    {
        var response = await fixture.AuthorizedClient.GetCharacterRawAsync(fixture.ProjectId, characterId: 999999);
        ((int)response.StatusCode).ShouldBeInRange(400, 599);
    }
}
