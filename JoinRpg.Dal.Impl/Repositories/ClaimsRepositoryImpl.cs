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
            .Where(c => c.MasterDeclinedDate == null && c.PlayerDeclinedDate == null && c.PlayerUserId == userId)
            .ToListAsync();
    }

    public Task<Project> GetClaims(int projectId)
    {
      return
        Ctx.ProjectsSet.Include(p => p.Claims)
          .Include(p => p.ProjectAcls)
          .SingleOrDefaultAsync(p => p.ProjectId == projectId);
    }
  }
}
