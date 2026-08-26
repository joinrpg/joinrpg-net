using JoinRpg.Common.PrimitiveTypes;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.ProjectMetadata;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.IntegrationTests.TestInfrastructure;
using JoinRpg.Services.Interfaces;
using JoinRpg.Services.Interfaces.Projects;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Сведения о заявках и игроке в ответе x-game-api. Проверяют путь через доменный агрегат
/// CharacterInfo (ADR013): статус занятости, состав групп и блок PlayerInfo.
/// </summary>
[Collection("XApi")]
public class XApiCharacterPlayerTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task CharacterWithoutClaims_HasNoPlayer()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);

        result.BusyStatus.ShouldBe(CharacterBusyStatus.NoClaims);
        result.PlayerInfo.ShouldBeNull();
#pragma warning disable CS0612 // Type or member is obsolete
        result.PlayerUserId.ShouldBeNull();
#pragma warning restore CS0612
    }

    [Fact]
    public async Task CharacterWithPendingClaim_IsDiscussed()
    {
        // Раньше этот статус был недостижим: CharacterView грузил только утверждённые заявки,
        // поэтому персонаж с заявкой в обсуждении выглядел как «нет заявок».
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var characterId = new CharacterIdentification(projectId, character.CharacterId);
        await OpenClaims(projectId);

        _ = await AddClaim(characterId, await CreatePlayer());

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);

        result.BusyStatus.ShouldBe(CharacterBusyStatus.Discussed);
        result.PlayerInfo.ShouldBeNull();
    }

    [Fact]
    public async Task CharacterWithApprovedClaim_HasPlayerInfo()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var characterId = new CharacterIdentification(projectId, character.CharacterId);
        await OpenClaims(projectId);

        var (playerId, playerEmail) = await CreatePlayerWithEmail();
        var claimId = await AddClaim(characterId, playerId);
        await ApproveClaim(claimId);

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);

        result.BusyStatus.ShouldBe(CharacterBusyStatus.HasPlayer);
        result.PlayerInfo.ShouldNotBeNull();
        result.PlayerInfo.PlayerUserId.ShouldBe(playerId.Value);
        result.PlayerInfo.PlayerContacts.Email.ShouldBe(playerEmail);
        // Взноса в проекте нет, поэтому заявка считается оплаченной полностью.
        result.PlayerInfo.PaidInFull.ShouldBeTrue();
#pragma warning disable CS0612 // Type or member is obsolete
        result.PlayerUserId.ShouldBe(playerId.Value);
#pragma warning restore CS0612
    }

    [Fact]
    public async Task UnverifiedVkIsNotReturned()
    {
        // Контракт PlayerContacts обещает отдавать только подтверждённый VK.
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());
        var characterId = new CharacterIdentification(projectId, character.CharacterId);
        await OpenClaims(projectId);

        var playerId = await CreatePlayer();
        await ApproveClaim(await AddClaim(characterId, playerId));

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);

        result.PlayerInfo.ShouldNotBeNull();
        result.PlayerInfo.PlayerContacts.VKontakte.ShouldBeNull();
    }

    [Fact]
    public async Task CharacterBelongsToRootGroup()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        var character = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());

        var result = await fixture.MasterClient.GetCharacterAsync(projectId, character.CharacterId);

        // Корневая группа спецгруппой не является, поэтому попадает и в Groups, и в AllGroups.
        result.Groups.ShouldNotBeEmpty();
        result.AllGroups.Select(g => g.CharacterGroupId)
            .ShouldBe(result.Groups.Select(g => g.CharacterGroupId), ignoreOrder: true);
    }

    private Task<UserIdentification> CreatePlayer()
        => TestUserProjectHelpers.CreateTestUserAsync(fixture.Factory.Services);

    private Task<(UserIdentification userId, string email)> CreatePlayerWithEmail()
        => TestUserProjectHelpers.CreateTestUserWithEmailAsync(fixture.Factory.Services);

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
            sp => sp.GetRequiredService<IClaimService>()
                .AddClaimFromUser(characterId, "Заявка от игрока", new Dictionary<int, string?>(), sensitiveDataAllowed: true));

    private Task ApproveClaim(ClaimIdentification claimId)
        => fixture.Factory.Services.RunAsAsync(
            fixture.MasterUserId,
            sp => sp.GetRequiredService<IClaimService>().ApproveByMaster(claimId, "Принято"));
}
