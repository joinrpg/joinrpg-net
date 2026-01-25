using JoinRpg.DataModel.Extensions;
using JoinRpg.Helpers;
using JoinRpg.PrimitiveTypes.Claims;
using LinqKit;

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

    public async Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId)
    {
        Expression<Func<CharacterGroup, GroupHeader>> groupHeaderSelector = group =>
            new GroupHeader()
            {
                IsActive = group.IsActive,
                CharacterGroupId = group.CharacterGroupId,
                CharacterGroupName = group.CharacterGroupName,
                IsSpecial = group.IsSpecial,
                ParentGroupIds = group.ParentGroupsImpl,
            };

        var character = await Ctx
            .Set<Character>()
            .AsNoTracking()
            .Include(c => c.Project.Details)
            .Where(e => e.CharacterId == characterId && e.ProjectId == projectId)
            .SingleOrDefaultAsync();

        var allGroups = await Ctx.Set<CharacterGroup>().AsNoTracking()
            .Where(cg => cg.ProjectId == projectId) // need to load inactive groups here
            .Select(groupHeaderSelector)
            .ToDictionaryAsync(d => d.CharacterGroupId);

        var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

        List<GroupHeader> directGroups = await Ctx.Set<CharacterGroup>()
                    .Where(group => character.ParentCharacterGroupIds.Contains(group.CharacterGroupId))
                    .Select(groupHeaderSelector)
                    .ToListAsync();

        var view = new CharacterView()
        {
            CharacterId = character.CharacterId,
            Name = character.CharacterName,
            Description = character.Description.Contents ?? "",
            UpdatedAt = character.UpdatedAt,
            IsActive = character.IsActive,
            InGame = character.InGame,
            CharacterTypeInfo = character.ToCharacterTypeInfo(),
            JsonData = character.JsonData,
            ApprovedClaim = await Ctx.Set<Claim>()
                .Where(claim => claim.CharacterId == characterId &&
                            claim.ClaimStatus == ClaimStatus.Approved)
                .Include(c => c.Player.Extra)
                .SingleOrDefaultAsync(),
            Claims = await Ctx.Set<Claim>().AsExpandable()
            .Where(claim => claim.CharacterId == characterId &&
                            claim.ClaimStatus == ClaimStatus.Approved).Select(
              claim => new ClaimHeader()
              {
                  IsActive = activeClaimPredicate.Invoke(claim),
              }).ToListAsync(),
            DirectGroups = directGroups,
            AllGroups = directGroups
            .SelectMany(g => g.FlatTree(group => group.ParentGroupIds._parentCharacterGroupIds.Select(id => allGroups[id])))
            .Where(g => g.IsActive)
            .Distinct()
            .ToList()
        };
        return view;
    }

    public async Task<IEnumerable<Character>> GetAvailableCharacters(int projectId)
    {
        return await Ctx.Set<Character>()
          .Where(c => c.ProjectId == projectId && c.IsAcceptingClaims && c.IsActive &&
                      !c.Project.Claims.Any(claim => (claim.ClaimStatus == ClaimStatus.Approved ||
                                        claim.ClaimStatus == ClaimStatus.CheckedIn) && claim.CharacterId == c.CharacterId))
          .OrderBy(c => c.CharacterName).ToListAsync();
    }

    public async Task<IEnumerable<Character>> GetAllCharacters(int projectId)
    {
        return await Ctx.Set<Character>()
          .Where(c => c.ProjectId == projectId)
          .OrderBy(c => c.CharacterName)
          .ToListAsync();
    }

    public async Task<IEnumerable<Character>> GetActiveTemplateCharacters(int projectId)
    {
        return await Ctx.Set<Character>()
          .Where(c => c.ProjectId == projectId)
          .Where(c => c.IsActive)
          .Where(c => c.CharacterType == PrimitiveTypes.CharacterType.Slot)
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
