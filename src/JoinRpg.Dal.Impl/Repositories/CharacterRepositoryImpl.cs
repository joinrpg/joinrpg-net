using JoinRpg.DomainTypes.Interfaces;

namespace JoinRpg.Dal.Impl.Repositories;

internal class CharacterRepositoryImpl(MyDbContext ctx) : GameRepositoryImplBase(ctx), ICharacterRepository
{
    public async Task<IReadOnlyCollection<CharacterHeader>> GetCharacterHeaders(int projectId, DateTime? modifiedSince)
    {
        return await Ctx.Set<Character>()
          .Where(c => c.ProjectId == projectId && c.IsActive &&
                                                     (modifiedSince == null ||
                                                      c.UpdatedAt >= modifiedSince))
          .Select(c => new CharacterHeader
          {
              IsActive = c.IsActive,
              CharacterId = c.CharacterId,
              UpdatedAt = c.UpdatedAt,
          }).ToListAsync();
    }

    public async Task<IReadOnlyCollection<Character>> GetCharacters(IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        var characterIntIds = characterIds.EnsureSameProject().Select(c => c.CharacterId).ToList();
        var projectId = characterIds.First().ProjectId;
        return
            await Ctx.Set<Character>()
            .Include(x => x.ApprovedClaim)
            .Where(cg => cg.ProjectId == projectId && characterIntIds.Contains(cg.CharacterId)).ToListAsync();
    }

    public async Task<Character> GetCharacterWithGroups(int projectId, int characterId)
    {
        await LoadProjectGroups(projectId);
        await LoadProjectFields(projectId);

        return
          await Ctx.Set<Character>().Include(ch => ch.ApprovedClaim!.Player)
            .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }
    public async Task<Character> GetCharacterWithDetails(int projectId, int characterId)
    {
        await LoadProjectGroups(projectId);

        return
          await Ctx.Set<Character>()
            .Include(ch => ch.ApprovedClaim)
            .Include(ch => ch.Claims)
            .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }




    public async Task<IEnumerable<Character>> GetAllCharacters(int projectId)
    {
        return await Ctx.Set<Character>()
          .Where(c => c.ProjectId == projectId)
          .OrderBy(c => c.CharacterName)
          .ToListAsync();
    }


    public async Task<Character> GetCharacterAsync(int projectId, int characterId)
    {
        await LoadProjectFields(projectId);
        await LoadProjectGroups(projectId);

        return
          await Ctx.Set<Character>()
            .Include(c => c.Project)
            .Include(c => c.Claims)
            .Include(c => c.ApprovedClaim)
            .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public Task<Character> GetCharacterAsync(CharacterIdentification characterId) => GetCharacterAsync(characterId.ProjectId, characterId.CharacterId);

    public async Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(IReadOnlyCollection<CharacterIdentification> characterIds)
    {
        characterIds.EnsureSameProject();

        if (characterIds.Count == 0)
        {
            return [];
        }

        var projectId = characterIds.First().ProjectId;
        var characterIntIds = characterIds.Select(x => x.CharacterId).ToArray();

        return await LoadCharactersWithGroupsImpl(projectId, e => characterIntIds.Contains(e.CharacterId));
    }

    private async Task<IReadOnlyCollection<Character>> LoadCharactersWithGroupsImpl(ProjectIdentification projectId, Expression<Func<Character, bool>> predicate)
    {
        await LoadProjectGroups(projectId);
        await LoadProjectClaimsAndComments(projectId);
        await LoadMasters(projectId);
        await LoadProjectFields(projectId);

        return
          await Ctx.Set<Character>()
            .Where(predicate)
            .Where(e => e.ProjectId == projectId).ToListAsync();
    }

    public async Task<IReadOnlyCollection<Character>> LoadCharactersWithGroups(ProjectIdentification projectId)
    {
        return await LoadCharactersWithGroupsImpl(projectId, e => true);
    }
}
