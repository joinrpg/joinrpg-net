using JoinRpg.Data.Interfaces.Characters;
using JoinRpg.DomainTypes.Characters;
using JoinRpg.DomainTypes.Characters.Claims;
using JoinRpg.DomainTypes.Interfaces;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories;

/// <summary>
/// Загрузчик <see cref="CharacterInfo"/> (ADR013).
/// </summary>
/// <remarks>
/// Тонкая обёртка над <see cref="CharacterInfoLoader"/>: сам запрос живёт там, здесь только
/// получение <see cref="ProjectInfo"/> из <see cref="IProjectMetadataRepository"/> (за ним
/// в Portal стоит кеш запроса).
///
/// Исключение — <see cref="GetCharactersForList"/>: он отдаёт не агрегат, а лёгкую проекцию,
/// не нуждается в <see cref="ProjectInfo"/> и к ядру загрузки <see cref="CharacterInfo"/>
/// отношения не имеет, поэтому свой запрос держит здесь.
/// </remarks>
internal class CharacterInfoRepository(MyDbContext ctx, IProjectMetadataRepository projectMetadataRepository)
    : ICharacterInfoRepository
{
    private readonly CharacterInfoLoader loader = new(ctx);

    public async Task<IReadOnlyCollection<CharacterListEntry>> GetCharactersForList(ProjectIdentification projectId)
    {
        var activeClaims = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        var query =
            from character in ctx.Set<Character>().AsNoTracking().AsExpandable()
            where character.ProjectId == projectId.Value
            select new
            {
                character.CharacterId,
                character.CharacterName,
                Description = character.Description.Contents,
                character.IsPublic,
                character.IsActive,
                character.CharacterType,
                character.IsHot,
                character.CharacterSlotLimit,
                character.HidePlayerForCharacter,
                character.ApprovedClaimId,
                ActiveClaimPlayerIds = character.Claims
                    .Where(claim => activeClaims.Invoke(claim))
                    .Select(claim => claim.PlayerUserId),
            };

        var rows = await query.ToListAsync();

        return [.. rows.Select(row => new CharacterListEntry(
            new CharacterIdentification(projectId, row.CharacterId),
            row.CharacterName,
            row.Description ?? "",
            row.IsPublic,
            row.IsActive,
            // Маппинг флагов живёт в одном месте — иначе он разойдётся с ToCharacterTypeInfo.
            CharacterTypeInfo.Create(
                row.CharacterType,
                row.IsHot,
                row.CharacterSlotLimit,
                row.CharacterName,
                row.IsPublic,
                row.HidePlayerForCharacter),
            ClaimIdentification.FromOptional(projectId, row.ApprovedClaimId),
            [.. row.ActiveClaimPlayerIds.Select(id => new UserIdentification(id))]))];
    }

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
