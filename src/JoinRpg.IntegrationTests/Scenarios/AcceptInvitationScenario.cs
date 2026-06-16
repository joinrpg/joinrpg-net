using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AcceptInvitationScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task MasterInvitesPlayer_PlayerAccepts_AutoAcceptApprovesClaim()
    {
        // 1. Создаём мастера, проект и игрока
        UserIdentification masterId;
        ProjectIdentification projectId;
        UserIdentification playerId;
        using (var scope = factory.Services.CreateScope())
        {
            masterId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
            projectId = await TestUserProjectHelpers.CreateProjectAsync(scope.ServiceProvider, masterId);
            playerId = await TestUserProjectHelpers.CreateTestUserAsync(scope.ServiceProvider);
        }

        // 2. Мастер включает автопринятие заявок и приём заявок, создаёт персонажа и приглашает игрока
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = true, IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new Dictionary<int, string?>()));
        });

        var claimId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            return await claimService.AddClaimFromMaster(characterId, playerId, "Приглашаем вас на роль", new Dictionary<int, string?>());
        });

        // Приглашение создаёт заявку в статусе «Предложена»
        (await GetClaimStatus(claimId)).ShouldBe(ClaimStatus.AddedByMaster);

        // 3. Игрок принимает приглашение
        await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.AcceptInvitation(claimId, "Согласен!", sensitiveDataAllowed: false);
        });

        // 4. Из-за включённого автопринятия заявка должна быть принята
        (await GetClaimStatus(claimId)).ShouldBe(ClaimStatus.Approved);
    }

    private async Task<ClaimStatus> GetClaimStatus(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        var claimsRepository = scope.ServiceProvider.GetRequiredService<IClaimsRepository>();
        var claim = await claimsRepository.GetClaim(claimId)
            ?? throw new InvalidOperationException("Claim not found");
        return claim.ClaimStatus;
    }
}
