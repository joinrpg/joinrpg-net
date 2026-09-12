using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
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
        await OpenClaims(projectId);

        var (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);
        var claimId = await AddClaim(characterId, playerId);
        await ApproveClaim(claimId);

        var result = await fixture.MasterClient.GetClaimAsync(projectId, claimId.ClaimId);

        result.ClaimId.ShouldBe(claimId.ClaimId);
        result.CharacterId.ShouldBe(character.CharacterId);
        result.PlayerContacts.Email.ShouldBe(playerEmail);
        result.Status.ShouldBe(ClaimStatusEnum.Approved);
    }

    /// <summary>Новый проект создаётся с закрытым приёмом заявок — открываем.</summary>
    private Task OpenClaims(ProjectIdentification projectId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                new ProjectClaimSettings(
                    DefaultTemplate: null,
                    StrictlyOneCharacter: false,
                    AutoAcceptClaims: false,
                    IsAcceptingClaims: true,
                    IsPublicProject: true)));

    /// <summary>
    /// Заявку подаёт сам игрок: заявку, добавленную мастером, мастер же утвердить не может —
    /// её сначала должен принять игрок.
    /// </summary>
    private Task<ClaimIdentification> AddClaim(CharacterIdentification characterId, UserIdentification playerId)
        => fixture.Factory.Services.RunAsAsync(
            playerId,
            async sp =>
            {
                var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>()
                    .GetProjectMetadata(characterId.ProjectId);
                return await sp.GetRequiredService<IClaimService>()
                    .AddClaimFromUser(
                        characterId, "Заявка от игрока", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: true);
            });

    private Task ApproveClaim(ClaimIdentification claimId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));
}
