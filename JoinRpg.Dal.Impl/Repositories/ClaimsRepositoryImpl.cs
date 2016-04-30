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
  public class ClaimsRepositoryImpl : GameRepositoryImplBase, IClaimsRepository
  {
    public ClaimsRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }

    public Task<ICollection<Claim>> GetClaims(int projectId) => GetClaimsImpl_(projectId);

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

    private async Task<ICollection<Claim>> GetClaimsImpl_(int projectId)
    {
      await LoadProjectCharactersAndGroups(projectId);
      await LoadMasters(projectId);
      await LoadProjectClaimsAndComments(projectId);
      await LoadProjectFields(projectId);

      //Sync operation, as anything should be loaded already
      return Ctx.ProjectsSet.Find(projectId).Claims;
    }

    public async Task<ICollection<Claim>> GetActiveClaimsForMaster(int projectId, int userId)
      => (await GetClaimsImpl_(projectId)).Where(c => c.ResponsibleMasterUserId == userId).ToList();

    public Task<Claim> GetClaim(int projectId, int claimId)
    {
      return
        Ctx.ClaimSet
          .Include(c => c.Project)
          .Include(c => c.Project.ProjectAcls)
          .Include(c => c.Character)
          .Include(c => c.Player)
          .Include(c => c.Player.Claims)
          .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
    }

    public async Task<Claim> GetClaimWithDetails(int projectId, int claimId)
    {
      await LoadMasters(projectId);
      await LoadProjectCharactersAndGroups(projectId);
      await LoadProjectClaims(projectId);

      return await
        Ctx.ClaimSet
          .Include(c => c.Comments.Select(com => com.Finance))
          .Include(c => c.Comments.Select(com => com.Author))
          .Include(c => c.Comments.Select(com => com.CommentText))
          .SingleOrDefaultAsync(e => e.ClaimId == claimId && e.ProjectId == projectId);
    }
  }
}
