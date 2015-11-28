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
          .Include(p => p.ProjectAcls.Select(a => a.User))
          .Include(p => p.Claims.Select(c => c.Comments))
          .Include(p => p.Claims.Select(c => c.Watermarks))
          .Include(p => p.Claims.Select(c => c.Player))
          .SingleOrDefaultAsync(p => p.ProjectId == projectId);
    }

    public async Task<IEnumerable<Claim>> GetMyClaimsForProject(int userId, int projectId)
      => await Ctx.ClaimSet.Where(c => c.ProjectId == projectId && c.PlayerUserId == userId).ToListAsync();
  }
}
