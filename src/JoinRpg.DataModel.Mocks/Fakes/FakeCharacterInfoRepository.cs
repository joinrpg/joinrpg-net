using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DataModel.Extensions;
using JoinRpg.Domain;
using JoinRpg.DomainTypes;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.ProjectMetadata;

namespace JoinRpg.DataModel.Mocks.Fakes;

/// <summary>
/// Загрузчик персонажей поверх мока — общий для всех тестовых проектов.
/// </summary>
/// <remarks>
/// Отдаёт ровно то, что отдал бы настоящий репозиторий, и ничего не фильтрует по доступности:
/// это задача доменных правил на стороне вызывающего (см. issue #4766).
/// </remarks>
public sealed class FakeCharacterInfoRepository(MockedProject mock) : ICharacterInfoRepository
{
    public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId)
        => Task.FromResult(Find(characterId));

    public Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId, ProjectInfo projectInfo)
        => Task.FromResult(Find(characterId));

    public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(IReadOnlyCollection<CharacterIdentification> characterIds)
        => Task.FromResult<IReadOnlyCollection<CharacterInfo>>(
            [.. All().Where(character => characterIds.Contains(character.Id))]);

    public Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(
        ProjectIdentification projectId,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
        => Task.FromResult<IReadOnlyCollection<CharacterInfo>>(
            [.. All().Where(character => character.DirectGroupIds.Any(groupIds.Contains))]);

    public Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId)
        => Task.FromResult<IReadOnlyCollection<CharacterInfo>>([.. All()]);

    public Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(ProjectIdentification projectId)
        => Task.FromResult<IReadOnlyCollection<CharacterListEntry>>(
            [.. mock.Project.Characters.Select(character => new CharacterListEntry(
                character.GetId(),
                character.CharacterName,
                character.Description?.Contents ?? "",
                character.IsPublic,
                character.IsActive,
                character.ToCharacterTypeInfo(),
                character.GetApprovedClaimIdOrDefault(),
                [.. character.Claims.Where(claim => claim.ClaimStatus.IsActive()).Select(claim => claim.GetPlayerId())]))]);

    private IEnumerable<CharacterInfo> All() => mock.Project.Characters.Select(mock.GetCharacterInfo);

    private CharacterInfo? Find(CharacterIdentification characterId)
        => All().SingleOrDefault(character => character.Id == characterId);
}
