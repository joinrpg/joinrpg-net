using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace JoinRpg.Dal.Impl.Repositories
{
  public class GameRepositoryImplBase : RepositoryImplBase
  {
    public GameRepositoryImplBase(MyDbContext ctx) : base(ctx)
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

    protected Task LoadProjectCharactersAndGroups(int projectId)
    {
      return Ctx.ProjectsSet
        .Include(p => p.CharacterGroups.Select(cg => cg.ParentGroups))
        .Include(p => p.Characters.Select(cg => cg.Groups))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }
  }
}
