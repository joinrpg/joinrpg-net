﻿using System;
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
  public class ClaimsRepositoryImpl : GameRepositoryImplBase, IClaimsRepository
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
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);
      await LoadProjectFields(projectId);

      Debug.WriteLine($"{nameof(LoadProjectClaimsAndComments)} started");
      return await Ctx
        .ClaimSet
        .Include(c => c.CommentDiscussion.Comments.Select(cm => cm.Finance))
        .Include(c => c.CommentDiscussion.Watermarks)
        .Include(c => c.Player)
        .Include(c => c.FinanceOperations)
        .Where(GetClaimStatusPredicate(status))
        .Where(predicate)
        .Where(
          c =>
            c.ProjectId == projectId
        ).ToListAsync();
    }

    public async Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, IReadOnlyCollection<int> claimindexes)
    {
      return
        await Ctx.ClaimSet.Include(c => c.Project.ProjectAcls.Select(pa => pa.User))
          .Include(c => c.Player)
          .Where(c => claimindexes.Contains(c.ClaimId) && c.ProjectId == projectid)
          .ToListAsync();
    }

    public Task<IReadOnlyCollection<Claim>> GetActiveClaimsForMaster(int projectId, int userId, ClaimStatusSpec status)
    {
      return GetClaimsImpl(projectId, status, claim => claim.ResponsibleMasterUserId == userId);
    }

    private Expression<Func<Claim, bool>> GetClaimStatusPredicate(ClaimStatusSpec status)
    {
      switch (status)
      {
        case ClaimStatusSpec.Any:
          return claim => true;
        case ClaimStatusSpec.Active:
          return c => c.ClaimStatus != Claim.Status.DeclinedByMaster && c.ClaimStatus != Claim.Status.DeclinedByUser &&
                      c.ClaimStatus != Claim.Status.OnHold;
        case ClaimStatusSpec.InActive:
          return c => !(c.ClaimStatus != Claim.Status.DeclinedByMaster && c.ClaimStatus != Claim.Status.DeclinedByUser &&
               c.ClaimStatus != Claim.Status.OnHold);
        case ClaimStatusSpec.Discussion:
          return c => c.ClaimStatus == Claim.Status.AddedByMaster || c.ClaimStatus == Claim.Status.AddedByUser || c.ClaimStatus == Claim.Status.Discussed;
        case ClaimStatusSpec.OnHold:
          return c => c.ClaimStatus == Claim.Status.OnHold;
        case ClaimStatusSpec.Approved:
          return c => c.ClaimStatus == Claim.Status.Approved;
        default:
          throw new ArgumentOutOfRangeException(nameof(status), status, null);
      }
    }

    public Task<Claim> GetClaim(int projectId, int? claimId)
    {
      return
        Ctx.ClaimSet.Include(c => c.Project)
          .Include(c => c.Project.ProjectAcls)
          .Include(c => c.Character)
          .Include(c => c.Player)
          .Include(c => c.Player.Claims)
          .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
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

    public Task<IReadOnlyCollection<Claim>> GetClaimsForPlayer(int projectId, ClaimStatusSpec claimStatusSpec, int userId)
    {
      return GetClaimsImpl(projectId, claimStatusSpec, claim => claim.PlayerUserId == userId);
    }
  }
}
