using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

[Collection("XApi")]
public class XApiClaimsTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task WithoutAuth_Returns401()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.AnonymousXApiClient.GetClaimAsync(fixture.ProjectId, claimId: 1));
    }

    [Fact]
    public async Task GetNonexistentClaim_ReturnsError()
    {
        await Should.ThrowAsync<HttpRequestException>(
            () => fixture.MasterClient.GetClaimAsync(fixture.ProjectId, claimId: 999999));
    }

    [Fact]
    public async Task MasterCanGetApprovedClaimDetails()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var characterId = new CharacterIdentification(projectId, character.CharacterId);
        await fixture.OpenClaims(projectId);

        var (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);
        var claimId = await fixture.AddClaimFromPlayer(characterId, playerId);
        await fixture.ApproveClaim(claimId);

        var result = await fixture.MasterClient.GetClaimAsync(projectId, claimId.ClaimId);

        result.ClaimId.ShouldBe(claimId.ClaimId);
        result.CharacterId.ShouldBe(character.CharacterId);
        result.PlayerContacts.Email.ShouldBe(playerEmail);
        result.Status.ShouldBe(ClaimStatusEnum.Approved);
    }
}
