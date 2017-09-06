using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using System.Data.Entity;
using System.Data.Entity.SqlServer;
using System.Diagnostics;
using System.Linq.Expressions;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  internal class ClaimsRepositoryImpl : GameRepositoryImplBase, IClaimsRepository
  {
    public ClaimsRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public Task<IReadOnlyCollection<Claim>> GetClaims(int projectId, ClaimStatusSpec status)
    {
      return GetClaimsImpl(projectId, status, claim  => true);
    }

    private async Task<IReadOnlyCollection<Claim>> GetClaimsImpl(int projectId, ClaimStatusSpec status, Expression<Func<Claim, bool>> predicate)
    {
      await LoadProjectFields(projectId);
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);

      Debug.WriteLine($"{nameof(LoadProjectClaimsAndComments)} started");
      return await Ctx
        .ClaimSet
        .Include(c => c.CommentDiscussion.Comments.Select(cm => cm.Finance))
        .Include(c => c.CommentDiscussion.Watermarks)
        .Include(c => c.Player)
        .Include(c => c.FinanceOperations)
        .Where(ClaimPredicates.GetClaimStatusPredicate(status))
        .Where(predicate)
        .Where(
          c =>
            c.ProjectId == projectId
        ).ToListAsync();
    }

    public async Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, IReadOnlyCollection<int> claimindexes)
    {
      await LoadProjectFields(projectid);
      return
        await Ctx.ClaimSet.Include(c => c.Project.ProjectAcls.Select(pa => pa.User))
          .Include(c => c.Player)
          .Where(c => claimindexes.Contains(c.ClaimId) && c.ProjectId == projectid)
          .ToListAsync();
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForMaster(int projectId, int userId, ClaimStatusSpec status)
    {
      return GetClaimsImpl(projectId, status, claim => claim.ResponsibleMasterUserId == userId);
    }

    public Task<Claim> GetClaim(int projectId, int? claimId) => GetClaimImpl(
      e => e.ClaimId == claimId && e.ProjectId == projectId);

    public Task<Claim> GetClaimByDiscussion(int projectId, int commentDiscussionId) => GetClaimImpl(
      e => e.CommentDiscussionId == commentDiscussionId && e.ProjectId == projectId);

    public async Task<IReadOnlyCollection<ClaimCountByMaster>> GetClaimsCountByMasters(int projectId, ClaimStatusSpec claimStatusSpec)
    {
      return await Ctx.Set<Claim>().Where(claim => claim.ProjectId == projectId)
        .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec)).GroupBy(claim => claim.ResponsibleMasterUserId)
        .Select(grouping => new ClaimCountByMaster() {ClaimCount = grouping.Count(), MasterId = grouping.Key})
        .ToListAsync();
    }

    public async Task<IReadOnlyCollection<ClaimWithPlayer>> GetClaimHeadersWithPlayer(int projectId, ClaimStatusSpec claimStatusSpec)
    {
      return await Ctx.Set<Claim>().Where(claim => claim.ProjectId == projectId)
        .Where(ClaimPredicates.GetClaimStatusPredicate(claimStatusSpec))
        .Include(c => c.Player.Extra)
        .Select(
          claim => new ClaimWithPlayer()
          {
            Player = claim.Player,
            ClaimId = claim.ClaimId,
            CharacterName = claim.Character.CharacterName,
            Extra =  claim.Player.Extra,
          })
        .ToListAsync();
    }

    private Task<Claim> GetClaimImpl(Expression<Func<Claim, bool>> predicate)
    {
      return
        Ctx.ClaimSet.Include(c => c.Project)
          .Include(c => c.Project.ProjectAcls)
          .Include(c => c.Character)
          .Include(c => c.Player)
          .Include(c => c.Player.Claims)
          .SingleOrDefaultAsync(predicate);
    }


    public async Task<Claim> GetClaimWithDetails(int projectId, int claimId)
    {
      await LoadProjectFields(projectId);
      await LoadMasters(projectId);
      await LoadProjectCharactersAndGroups(projectId);
      await LoadProjectClaims(projectId);

      return
        await
          Ctx.ClaimSet.Include(c => c.CommentDiscussion.Comments.Select(com => com.Finance))
            .Include(c => c.CommentDiscussion.Comments.Select(com => com.Author))
            .Include(c => c.CommentDiscussion.Comments.Select(com => com.CommentText))
            .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForGroups(int projectId, ClaimStatusSpec active, int[] characterGroupsIds)
    {
      return GetClaimsImpl(projectId, active,
        claim => (claim.CharacterGroupId != null && characterGroupsIds.Contains(claim.CharacterGroupId.Value))
                 ||
                 (claim.CharacterId != null &&
                 characterGroupsIds.Any(id => SqlFunctions.CharIndex(id.ToString(), claim.Character.ParentGroupsImpl.ListIds) > 0
                  )));
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForGroupDirect(int projectId, ClaimStatusSpec active, int characterGroupsId)
    {
      return GetClaimsImpl(projectId, active, claim => claim.CharacterGroupId == characterGroupsId);
    }

    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId)
    {
      return GetClaimsImpl(projectId, claimStatusSpec, claim => claim.PlayerUserId == userId);
    }
  }
}
