using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using System.Data.Entity;

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

    public Task<ICollection<Claim>> GetActiveClaims(int projectId) => GetClaimsForPredicate(projectId);

    private async Task<ICollection<Claim>> GetClaimsForPredicate(int projectId)
    {
      //It's too hard to load in one query, esp. because of Characters ←→ Claim links. LINQ don't know that if we load all claims for project
      //including characters, we will have all claims for this characters (so Project.Claims.Characters.Claims doesn't require loading claims second time
      await Ctx.ProjectsSet
        .Include(p => p.Details)
        .Include(p => p.Characters.Select(ch => ch.Claims))
        .Include(p => p.CharacterGroups)
        .Include(p => p.ProjectAcls.Select(a => a.User))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();

      return await
        Ctx.ClaimSet    
          .Include(c => c.Comments.Select(cm => cm.Finance))
          .Include(c => c.Watermarks)
          .Include(c => c.Player)
          .Include(c => c.FinanceOperations)
          .Where(c => c.ProjectId == projectId).ToListAsync();
    }

    public async Task<ICollection<Claim>> GetActiveClaimsForMaster(int projectId, int userId)
      => (await GetClaimsForPredicate(projectId)).Where(c => c.ResponsibleMasterUserId == userId).ToList();
  }
}
