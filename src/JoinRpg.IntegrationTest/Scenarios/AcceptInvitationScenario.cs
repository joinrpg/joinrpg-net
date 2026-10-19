using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Characters;
using JoinRpg.Services.Interfaces.Projects;

namespace JoinRpg.IntegrationTest.Scenarios;

public class AcceptInvitationScenario(JoinApplicationFactory factory) : IClassFixture<JoinApplicationFactory>
{
    /// <summary>
    /// Автоприём работает только для заявок на шаблон (#5645): принятая заявка переезжает на
    /// нового персонажа, созданного из шаблона.
    /// </summary>
    [Fact]
    public async Task MasterInvitesPlayerToSlot_PlayerAccepts_AutoAcceptApprovesClaim()
    {
        var (claimId, slotId) = await InviteAndAccept(
            new CharacterTypeInfo(CharacterType.Slot, IsHot: false, SlotLimit: null, SlotName: "Стражник", CharacterVisibility.Public));

        // Из-за включённого автопринятия заявка должна быть принята
        var claim = await GetClaim(claimId);
        claim.ClaimStatus.ShouldBe(ClaimStatus.Approved);
        claim.CharacterId.ShouldNotBe(slotId.CharacterId);
    }

    /// <summary>
    /// Заявку на обычного персонажа автоприём не трогает, даже если он включён в проекте (#5645).
    /// </summary>
    [Fact]
    public async Task MasterInvitesPlayerToCharacter_PlayerAccepts_AutoAcceptDoesNotApprove()
    {
        var (claimId, characterId) = await InviteAndAccept(
            new CharacterTypeInfo(CharacterType.Player, IsHot: false, SlotLimit: null, SlotName: null, CharacterVisibility.Public));

        var claim = await GetClaim(claimId);
        claim.ClaimStatus.ShouldBe(ClaimStatus.Discussed);
        claim.CharacterId.ShouldBe(characterId.CharacterId);
    }

    private async Task<(ClaimIdentification ClaimId, CharacterIdentification CharacterId)> InviteAndAccept(
        CharacterTypeInfo characterType)
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
                characterType,
                FieldValues: FieldLayerContainer.Empty(projectInfo)));
        });

        var claimId = await factory.Services.RunAsAsync(masterId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            var projectInfo = await sp.GetRequiredService<IProjectMetadataRepository>().GetProjectMetadata(projectId);
            return await claimService.AddClaimFromMaster(
                characterId, playerId, "Приглашаем вас на роль", FieldLayerContainer.Empty(projectInfo));
        });

        // Приглашение создаёт заявку в статусе «Предложена»
        (await GetClaim(claimId)).ClaimStatus.ShouldBe(ClaimStatus.AddedByMaster);

        // 3. Игрок принимает приглашение
        await factory.Services.RunAsAsync(playerId, async sp =>
        {
            var claimService = sp.GetRequiredService<IClaimService>();
            await claimService.AcceptInvitation(claimId, "Согласен!", sensitiveDataAllowed: false);
        });

        return (claimId, characterId);
    }

    private async Task<DataModel.Claim> GetClaim(ClaimIdentification claimId)
    {
        using var scope = factory.Services.CreateScope();
        var claimsRepository = scope.ServiceProvider.GetRequiredService<IClaimsRepository>();
        return await claimsRepository.GetClaim(claimId)
            ?? throw new InvalidOperationException("Claim not found");
    }
}
