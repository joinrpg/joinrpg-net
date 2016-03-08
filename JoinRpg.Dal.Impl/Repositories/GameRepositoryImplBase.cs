using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace JoinRpg.Dal.Impl.Repositories
{
  public class GameRepositoryImplBase : RepositoryImplBase
  {
    protected GameRepositoryImplBase(MyDbContext ctx) : base(ctx)
    {
    }

    protected Task LoadMasters(int projectId)
    {
      return Ctx.ProjectsSet
        .Include(p => p.ProjectAcls.Select(acl => acl.User))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }

    protected Task LoadProjectClaims(int projectId)
    {
      return Ctx.ProjectsSet.Include(p => p.Claims.Select(c => c.Player)).Where(p => p.ProjectId == projectId).LoadAsync();
    }

    protected Task LoadProjectClaimsAndComments(int projectId)
    {
      return Ctx.ClaimSet
        .Include(c => c.Comments.Select(cm => cm.Finance))
        .Include(c => c.Watermarks)
        .Include(c => c.Player)
        .Include(c => c.FinanceOperations)
        .Where(c => c.ProjectId == projectId).LoadAsync();
    }

    protected Task LoadProjectCharactersAndGroups(int projectId)
    {
      return Ctx.ProjectsSet
        .Include(p => p.CharacterGroups.Select(cg => cg.ParentGroups))
        .Include(p => p.Characters.Select(cg => cg.Groups))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }

    protected Task LoadProjectGroups(int projectId)
    {
      return Ctx.ProjectsSet
        .Include(p => p.CharacterGroups.Select(cg => cg.ParentGroups))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }
  }
}
