using System.Net;
using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;
using Microsoft.AspNetCore.Mvc.Testing;

namespace JoinRpg.IntegrationTest.Scenarios;

/// <summary>
/// Панели мастерских действий на странице заявки выводятся только тому, кто может ими
/// воспользоваться. Раньше игрок получал скрытую панель перемещения заявки с островом
/// <c>AvailClaimTargets</c>: остров запрашивал мастерский список персонажей, получал 403 и
/// валил рендер WASM на странице игрока.
/// </summary>
public class ClaimPageMasterPanelsScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    private const string MoveClaimIsland = "JoinRpg.Web.Claims.AvailClaimTargets";

    [Fact]
    public async Task Player_DoesNotGetMasterOnlyIsland()
    {
        var (projectId, claimId, _, playerEmail) = await CreateClaim();

        var html = await GetClaimPage(playerEmail, projectId, claimId);

        html.ShouldNotContain(MoveClaimIsland);
    }

    [Fact]
    public async Task Master_GetsMoveClaimIsland()
    {
        var (projectId, claimId, masterEmail, _) = await CreateClaim();

        var html = await GetClaimPage(masterEmail, projectId, claimId);

        html.ShouldContain(MoveClaimIsland);
    }

    private async Task<(ProjectIdentification projectId, ClaimIdentification claimId, string masterEmail, string playerEmail)> CreateClaim()
    {
        UserIdentification masterId;
        string masterEmail;
        ProjectIdentification projectId;
        UserIdentification playerId;
        string playerEmail;
        using (var scope = factory.Services.CreateScope())
        {
            (masterId, masterEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            (playerId, playerEmail) = await TestUserProjectHelpers.CreateTestUserWithEmailAsync(scope.ServiceProvider);
        }

        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            await sp.GetRequiredService<IProjectService>().SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            return await sp.GetRequiredService<ICharacterService>().AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await sp.GetRequiredService<IClaimService>().AddClaimFromUser(
                characterId, "Хочу эту роль", FieldLayerContainer.Empty(projectInfo), sensitiveDataAllowed: false);
        });

        return (projectId, claimId, masterEmail, playerEmail);
    }

    private async Task<string> GetClaimPage(string email, ProjectIdentification projectId, ClaimIdentification claimId)
    {
        var client = await TestUserProjectHelpers.CreateAuthenticatedClientAsync(
            factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false }),
            email,
            followsRedirects: false);

        var response = await client.GetAsync($"{projectId.Value}/claim/{claimId.ClaimId}/edit");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        return await response.Content.ReadAsStringAsync();
    }
}
