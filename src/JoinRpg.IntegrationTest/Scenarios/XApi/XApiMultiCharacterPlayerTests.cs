using JoinRpg.DomainTypes;
using JoinRpg.IntegrationTest.TestInfrastructure;
using JoinRpg.XGameApi.Contract;

namespace JoinRpg.IntegrationTest.Scenarios.XApi;

/// <summary>
/// Многоперсонажные ответы x-game-api (by-ids и groups/{groupId}/characters), когда игрок есть
/// больше чем у одного персонажа.
/// </summary>
/// <remarks>
/// Ровно этот случай раньше падал: маппинг шёл через Task.WhenAll, а игрока каждый персонаж
/// догружал сам, и EF6-контекст (не потокобезопасный) отвечал «A second operation started on this
/// context». При нуле или одном игроке перекрытия не возникает — поэтому остальные тесты,
/// дёргающие одиночный GetCharacter, баг не видели. Проверяем ещё и то, что игроки не
/// перепутались между персонажами: пакетная загрузка обязана разложить их по своим.
/// </remarks>
[Collection("XApi")]
public class XApiMultiCharacterPlayerTests(XApiMasterFixture fixture)
{
    [Fact]
    public async Task ByIds_TwoCharactersWithPlayers_EachHasItsOwnPlayer()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        await fixture.OpenClaims(projectId);

        var first = await fixture.CreateCharacterWithPlayer(projectId);
        var second = await fixture.CreateCharacterWithPlayer(projectId);

        var result = await fixture.MasterClient.GetCharactersByIdsAsync(
            projectId, first.CharacterId, second.CharacterId);

        result.Select(c => c.CharacterId)
            .ShouldBe([first.CharacterId, second.CharacterId], ignoreOrder: true);
        AssertPlayer(result, first);
        AssertPlayer(result, second);
    }

    [Fact]
    public async Task ByGroup_TwoCharactersWithPlayers_EachHasItsOwnPlayer()
    {
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        await fixture.OpenClaims(projectId);

        var first = await fixture.CreateCharacterWithPlayer(projectId);
        var second = await fixture.CreateCharacterWithPlayer(projectId);
        var groupId = await GetRootGroupId(projectId, first.CharacterId);

        var result = await fixture.MasterClient.GetGroupCharactersAsync(projectId, groupId);

        // В корневой группе лежит ещё и шаблон «Хочу на игру», который создаётся вместе с проектом, —
        // нам важно лишь, что оба сыгранных персонажа пришли в одном ответе.
        result.Select(c => c.CharacterId).ShouldContain(first.CharacterId);
        result.Select(c => c.CharacterId).ShouldContain(second.CharacterId);
        AssertPlayer(result, first);
        AssertPlayer(result, second);
    }

    [Fact]
    public async Task ByIds_CharacterWithoutClaimMixedIn_HasNoPlayerInfo()
    {
        // Смешанный случай: пакетная загрузка игроков не должна ломаться на персонаже без заявки.
        var projectId = await fixture.CreateNewProject(fixture.MasterUserId);
        await fixture.OpenClaims(projectId);

        var first = await fixture.CreateCharacterWithPlayer(projectId);
        var second = await fixture.CreateCharacterWithPlayer(projectId);
        var vacant = await fixture.MasterClient.CreateCharacterAsync(projectId, new CreateCharacterRequest());

        var result = await fixture.MasterClient.GetCharactersByIdsAsync(
            projectId, first.CharacterId, vacant.CharacterId, second.CharacterId);

        AssertPlayer(result, first);
        AssertPlayer(result, second);

        var vacantResult = result.Single(c => c.CharacterId == vacant.CharacterId);
        vacantResult.BusyStatus.ShouldBe(CharacterBusyStatus.NoClaims);
        vacantResult.PlayerInfo.ShouldBeNull();
    }

    private static void AssertPlayer(IReadOnlyList<CharacterInfo> result, CharacterWithPlayer expected)
    {
        var character = result.Single(c => c.CharacterId == expected.CharacterId);
        character.BusyStatus.ShouldBe(CharacterBusyStatus.HasPlayer);
        character.PlayerInfo.ShouldNotBeNull();
        character.PlayerInfo.PlayerUserId.ShouldBe(expected.PlayerId.Value);
        character.PlayerInfo.PlayerContacts.Email.ShouldBe(expected.PlayerEmail);
    }

    /// <summary>Корневая группа проекта — единственная, в которой лежат все новые персонажи.</summary>
    private async Task<int> GetRootGroupId(ProjectIdentification projectId, int characterId)
    {
        var character = await fixture.MasterClient.GetCharacterAsync(projectId, characterId);
        return character.Groups.Single().CharacterGroupId;
    }
}
