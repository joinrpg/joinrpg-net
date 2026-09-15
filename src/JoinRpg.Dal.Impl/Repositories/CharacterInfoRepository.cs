using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Загрузчик <see cref="CharacterInfo"/> (ADR013).
/// </summary>
/// <remarks>
/// Тонкая обёртка над <see cref="CharacterInfoLoader"/>: сам запрос живёт там, здесь только
/// получение <see cref="ProjectInfo"/> из <see cref="IProjectMetadataRepository"/> (за ним
/// в Portal стоит кеш запроса).
/// </remarks>
internal class CharacterInfoRepository(MyDbContext ctx, IProjectMetadataRepository projectMetadataRepository)
    : ICharacterInfoRepository
{
    private readonly CharacterInfoLoader loader = new(ctx);

    public async Task<CharacterInfo?> GetCharacterInfoOrDefault(CharacterIdentification characterId)
    {
        var characterIntId = characterId.CharacterId;
        var result = await GetCoreAsync(characterId.ProjectId, character => character.CharacterId == characterIntId);
        return result.SingleOrDefault();
    }

    public async Task<CharacterInfo?> GetCharacterInfoOrDefault(
        CharacterIdentification characterId,
        ProjectInfo projectInfo)
    {
        var characterIntId = characterId.CharacterId;
        var result = await loader.LoadAsync(projectInfo, character => character.CharacterId == characterIntId);
        return result.SingleOrDefault();
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfos(
        IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        if (characterIds.Count == 0)
        {
            return [];
        }

        var projectId = characterIds.EnsureSameProject().First().ProjectId;
        var characterIntIds = characterIds.Select(c => c.CharacterId).ToArray();

        return await GetCoreAsync(projectId, character => characterIntIds.Contains(character.CharacterId));
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetCharacterInfosByGroups(
        ProjectIdentification projectId,
        IReadOnlyCollection<CharacterGroupIdentification> groupIds)
    {
        if (groupIds.Count == 0)
        {
            return [];
        }

        return await GetCoreAsync(projectId, CharacterPredicates.ByGroup(groupIds));
    }

    public async Task<IReadOnlyCollection<CharacterInfo>> GetAllCharacterInfos(ProjectIdentification projectId)
        => await GetCoreAsync(projectId, character => true);

    private async Task<IReadOnlyCollection<CharacterInfo>> GetCoreAsync(
        ProjectIdentification projectId,
        Expression<Func<Character, bool>> predicate)
    {
        // Метаданные проекта берутся из своего репозитория (за ним в Portal стоит кеш запроса).
        var projectInfo = await projectMetadataRepository.GetProjectMetadata(projectId);

        return await loader.LoadAsync(projectInfo, predicate);
    }
}
