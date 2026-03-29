using System.Net;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiCreateCharacterTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task CreatePlayerCharacter_Success()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.Player,
        });
        header.CharacterId.ShouldBePositive();
        header.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateNonPlayerCharacter_Success()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.NonPlayer,
        });
        header.CharacterId.ShouldBePositive();
        header.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateSlotCharacter_Success()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var header = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.Slot,
            SlotName = "Тестовый слот",
        });
        header.CharacterId.ShouldBePositive();
        header.IsActive.ShouldBeTrue();
    }

    [Fact]
    public async Task CreateNonPlayerHot_ReturnsBadRequest()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var response = await fixture.MasterClient.CreateCharacterRawAsync(projectId, new CreateCharacterRequest
        {
            CharacterType = CharacterTypeApi.NonPlayer,
            IsHot = true,
        });
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Theory]
    [InlineData(ProjectTypeDto.Larp)]
    [InlineData(ProjectTypeDto.Convention)]
    [InlineData(ProjectTypeDto.ConventionProgram)]
    [InlineData(ProjectTypeDto.EmptyProject)]
    public async Task CreateCharacter_AllProjectTypes_Success(ProjectTypeDto projectType)
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId, projectType);
        var header = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        header.CharacterId.ShouldBePositive();
    }

    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var response = await fixture.AnonymousXApiClient.CreateCharacterRawAsync(projectId, new CreateCharacterRequest());
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
