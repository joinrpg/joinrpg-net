using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using LinqKit;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  internal class CharacterRepositoryImpl : GameRepositoryImplBase, ICharacterRepository
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

    public async Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, IReadOnlyCollection<int> characterIds)
    {
      return await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();
    }

    public async Task<Character> GetCharacterWithGroups(int projectId, int characterId)
    {
      await LoadProjectGroups(projectId);
      await LoadProjectFields(projectId);

      return
        await Ctx.Set<Character>().Include(ch => ch.ApprovedClaim.Player)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }
    public async Task<Character> GetCharacterWithDetails(int projectId, int characterId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadProjectClaims(projectId);
      await LoadProjectFields(projectId);

      return
        await Ctx.Set<Character>().Include(ch => ch.ApprovedClaim)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public async Task<CharacterView> GetCharacterViewAsync(int projectId, int characterId)
    {
      var character = await Ctx.Set<Character>().AsNoTracking()
        .Where(e => e.CharacterId == characterId && e.ProjectId == projectId)
        .SingleOrDefaultAsync();

      var activeClaimPredicate = ClaimPredicates.GetClaimStatusPredicate(ClaimStatusSpec.Active);

      return
        new CharacterView()
        {
          CharacterId = character.CharacterId,
          UpdatedAt = character.UpdatedAt,
          IsActive = character.IsActive,
          InGame = character.InGame,
          IsAcceptingClaims = character.IsAcceptingClaims,
          JsonData = character.JsonData,
          ApprovedClaim = await Ctx.Set<Claim>()
            .Where(claim => claim.CharacterId == characterId &&
                            claim.ClaimStatus == Claim.Status.Approved).Select(
              claim => new ClaimView()
              {
                PlayerUserId = claim.PlayerUserId,
                JsonData = claim.JsonData,
              }).SingleOrDefaultAsync(),
          Claims = await Ctx.Set<Claim>().AsExpandable()
            .Where(claim => claim.CharacterId == characterId &&
                            claim.ClaimStatus == Claim.Status.Approved).Select(
              claim => new ClaimHeader()
              {
                IsActive = activeClaimPredicate.Invoke(claim), 
              }).ToListAsync(),
          Groups = await Ctx.Set<CharacterGroup>()
            .Where(group => character.ParentCharacterGroupIds.Contains(group.CharacterGroupId))
            .Select(group => new GroupHeader()
            {
              IsActive = group.IsActive,
              CharacterGroupId = group.CharacterGroupId,
              CharacterGroupName = group.CharacterGroupName,
              IsSpecial = group.IsSpecial,
            }).ToListAsync(),
        };
    }

    public async Task<IEnumerable<Character>> GetAvailableCharacters(int projectId)
    {
      return await Ctx.Set<Character>()
        .Where(c => c.ProjectId == projectId && c.IsAcceptingClaims && c.IsActive &&
                    !c.Project.Claims.Any(claim => (claim.ClaimStatus == Claim.Status.Approved ||
                                      claim.ClaimStatus == Claim.Status.CheckedIn) && claim.CharacterId == c.CharacterId))
        .OrderBy(c => c.CharacterName).ToListAsync();
    }

    public async Task<Character> GetCharacterAsync(int projectId, int characterId)
    {
      await LoadProjectFields(projectId);
      return
        await Ctx.Set<Character>()
          .Include(c => c.Project)
          .SingleOrDefaultAsync(e => e.CharacterId == characterId && e.ProjectId == projectId);
    }

    public CharacterRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }
  }
}
