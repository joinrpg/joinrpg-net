using JoinRpg.IntegrationTest.TestInfrastructure;

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
        var characterId = await fixture.CreateNewCharacter(fixture.MasterUserId, newProjectId);
        var result = await fixture.MasterClient.GetCharacterAsync(newProjectId, characterId.CharacterId);
        result.CharacterId.ShouldBe(characterId.CharacterId);
    }
}
