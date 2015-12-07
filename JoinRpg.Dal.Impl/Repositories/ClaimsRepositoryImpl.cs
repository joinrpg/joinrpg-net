using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using System.Data.Entity;
using System.Linq.Expressions;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class ClaimsRepositoryImpl : RepositoryImplBase, IClaimsRepository
  {
    public ClaimsRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public async Task<IEnumerable<Claim>> GetActiveClaimsForUser(int userId)
    {
      return
        await
          Ctx.ClaimSet.Include(p => p.Character)
            .Include(p => p.Group)
            .Where(
              c =>
                c.ClaimStatus != Claim.Status.DeclinedByMaster && c.ClaimStatus != Claim.Status.DeclinedByUser &&
                c.PlayerUserId == userId)
            .ToListAsync();
    }

    public Task<Project> GetClaims(int projectId)
    {
      return
        Ctx.ProjectsSet.Include(p => p.Claims)
          .Include(p => p.ProjectAcls)
          .Include(p => p.CharacterGroups)
          .Include(p => p.Characters)
          .Include(p => p.ProjectAcls.Select(a => a.User))
          .Include(p => p.Claims.Select(c => c.Comments))
          .Include(p => p.Claims.Select(c => c.Watermarks))
          .Include(p => p.Claims.Select(c => c.Player))
          .SingleOrDefaultAsync(p => p.ProjectId == projectId);
    }

    public async Task<IEnumerable<Claim>> GetMyClaimsForProject(int userId, int projectId)
      => await Ctx.ClaimSet.Where(c => c.ProjectId == projectId && c.PlayerUserId == userId).ToListAsync();

    public async Task<IEnumerable<Claim>> GetClaimsByIds(int projectid, ICollection<int> claimindexes)
    {
      return
        await Ctx.ClaimSet.Include(c => c.Project.ProjectAcls.Select(pa => pa.User))
          .Include(c => c.Player)
          .Where(c => claimindexes.Contains(c.ClaimId))
          .ToListAsync();
    }

    public Task<IEnumerable<Claim>> GetActiveClaims(int projectId) => GetClaimsForPredicate(p => p.ProjectId == projectId);

    private async Task<IEnumerable<Claim>> GetClaimsForPredicate(Expression<Func<Claim, bool>> expression)
    {
      return await
        Ctx.ClaimSet.
          Include(c => c.Project)
          .Include(c => c.Project.ProjectAcls)
          .Include(c => c.Project.CharacterGroups)
          .Include(c => c.Project.Characters)
          .Include(c => c.Project.ProjectAcls.Select(a => a.User))
          .Include(c => c.Comments)
          .Include(c => c.Watermarks)
          .Include(c => c.Player)
          .Where(expression).ToListAsync();
    }

    public Task<IEnumerable<Claim>> GetActiveClaimsForMaster(int projectId, int userId)
      => GetClaimsForPredicate(p => p.ProjectId == projectId && p.ResponsibleMasterUserId == userId);
  }
}
