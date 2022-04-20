using System.Data.Entity;
using System.Linq.Expressions;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.Data.Interfaces.Claims;
using JoinRpg.DataModel;
using JoinRpg.Helpers;
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

        public async Task<IReadOnlyCollection<Character>> GetCharacters(int projectId, IReadOnlyCollection<int> characterIds) => await Ctx.Set<Character>().Where(cg => cg.ProjectId == projectId && characterIds.Contains(cg.CharacterId)).ToListAsync();

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
            await LoadProjectCharactersAndGroups(projectId);
            await LoadProjectClaims(projectId);
            await LoadProjectFields(projectId);

            return
              await Ctx.Set<Character>().Include(ch => ch.ApprovedClaim)
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

            var view =
            new CharacterView()
            {
                CharacterId = character.CharacterId,
                Name = character.CharacterName,
                Description = character.Description.Contents,
                UpdatedAt = character.UpdatedAt,
                IsActive = character.IsActive,
                InGame = character.InGame,
                CharacterTypeInfo = new(character.CharacterType, character.IsHot, character.CharacterSlotLimit),
                JsonData = character.JsonData,
                ApprovedClaim = await Ctx.Set<Claim>()
                    .Where(claim => claim.CharacterId == characterId &&
                                claim.ClaimStatus == Claim.Status.Approved)
                    .Include(c => c.Player.Extra)
                    .SingleOrDefaultAsync(),
                Claims = await Ctx.Set<Claim>().AsExpandable()
                .Where(claim => claim.CharacterId == characterId &&
                                claim.ClaimStatus == Claim.Status.Approved).Select(
                  claim => new ClaimHeader()
                  {
                      IsActive = activeClaimPredicate.Invoke(claim),
                  }).ToListAsync(),
                DirectGroups = await Ctx.Set<CharacterGroup>()
                .Where(group => character.ParentCharacterGroupIds.Contains(group.CharacterGroupId))
                .Select(groupHeaderSelector).ToListAsync(),
                LegacyNameMode = character.Project.Details.CharacterNameLegacyMode,
            };

            view.AllGroups = view.DirectGroups
                .SelectMany(g => g.FlatTree(group => group.ParentGroupIds._parentCharacterGroupIds.Select(id => allGroups[id])))
                .Where(g => g.IsActive)
                .Distinct()
                .ToList();
            return view;
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
