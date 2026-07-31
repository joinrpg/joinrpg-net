using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class RestoreClaimByMasterScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    [Fact]
    public async Task MasterRestoresDeclinedClaim_ClaimBecomesInvitedWithoutContactsAccess()
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

        // 2. Мастер включает приём заявок (без автопринятия), создаёт персонажа
        var characterId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var projectService = sp.GetRequiredService<IProjectService>();
            var metadataRepository = sp.GetRequiredService<IProjectMetadataRepository>();
            var projectInfo = await metadataRepository.GetProjectMetadata(projectId);
            await projectService.SetClaimSettings(
                projectId,
                projectInfo.ClaimSettings with { AutoAcceptClaims = false, IsAcceptingClaims = true });

            var characterService = sp.GetRequiredService<ICharacterService>();
            return await characterService.AddCharacter(new AddCharacterRequest(
                projectId,
                ParentCharacterGroupIds: [],
                new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public),
                FieldValues: new Dictionary<int, string?>()));
        });

        // 3. Игрок подаёт заявку, мастер принимает и затем отклоняет её
        var claimId = await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            return await claimService.AddClaimFromUser(characterId, "Хочу эту роль", new Dictionary<int, string?>(), sensitiveDataAllowed: false);
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.ApproveByMaster(claimId, "Принято");
        });

        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.DeclineByMaster(claimId, ClaimDenialReason.Removed, "Отклонено", deleteCharacter: false);
        });

        // 4. Мастер восстанавливает заявку
        await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.RestoreByMaster(claimId, "Восстановлено", characterId);
        });

        // 5. Заявка должна быть в статусе «Предложена» (приглашение) без доступа к контактам
        var restoredClaim = await GetClaim(claimId);
        restoredClaim.ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);
        restoredClaim.PlayerAllowedSenstiveData.ShouldBeFalse();
    }

    private async Task<Claim> GetClaim(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        var claimsRepository = scope.ServiceProvider.GetRequiredService<IClaimsRepository>();
        return await claimsRepository.GetClaim(claimId)
            ?? throw new InvalidOperationException("Claim not found");
    }
}
