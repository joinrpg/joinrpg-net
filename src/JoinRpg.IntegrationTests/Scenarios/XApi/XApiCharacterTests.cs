using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCharacterTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetCharactersAsync(fixture.ProjectId));
    }

    [Fact]
    public async Task MasterCanGetEmptyCharacterList()
    {
        var characters = await fixture.MasterClient.GetCharactersAsync(fixture.ProjectId);
        characters.ShouldNotBeNull();
    }

    [Fact]
    public async Task GetNonexistentCharacter_ReturnsError()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.MasterClient.GetCharacterAsync(fixture.ProjectId, characterId: 999999));
    }

    [Fact]
    public async Task CanCreateCharacter()
    {
        var newProjectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(newProjectId, new CreateCharacterRequest());
        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, header.CharacterId);
        result.CharacterId.ShouldBe(header.CharacterId);
    }
}
