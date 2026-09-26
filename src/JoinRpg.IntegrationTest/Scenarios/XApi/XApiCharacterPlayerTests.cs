using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
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
        await fixture.OpenClaims(projectId);

        _ = await fixture.AddClaim(characterId, await fixture.CreatePlayer());

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
        await fixture.OpenClaims(projectId);

        var (playerId, playerEmail) = await fixture.CreatePlayerWithEmail();
        var claimId = await fixture.AddClaim(characterId, playerId);
        await fixture.ApproveClaim(claimId);

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
        await fixture.OpenClaims(projectId);

        var playerId = await fixture.CreatePlayer();
        await fixture.ApproveClaim(await fixture.AddClaim(characterId, playerId));

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
}
