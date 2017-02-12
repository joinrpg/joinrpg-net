using System.Data.Entity;
using System.Diagnostics;
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
      Debug.WriteLine($"{nameof(LoadMasters)} started");
      return Ctx.ProjectsSet
        .Include(p => p.ProjectAcls.Select(acl => acl.User))
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }

    protected Task LoadProjectClaims(int projectId)
    {
      Debug.WriteLine($"{nameof(LoadProjectClaims)} started");
      return Ctx.ProjectsSet.Include(p => p.Claims.Select(c => c.Player)).Where(p => p.ProjectId == projectId).LoadAsync();
    }

    protected Task LoadProjectClaimsAndComments(int projectId)
    {
      Debug.WriteLine($"{nameof(LoadProjectClaimsAndComments)} started");
      return Ctx
        .ProjectsSet
        .Include(p => p.Claims.Select(c => c.CommentDiscussion.Comments.Select(cm => cm.Finance)))
        .Include(p => p.Claims.Select(c => c.CommentDiscussion.Watermarks))
        .Include(p => p.Claims.Select(c => c.Player))
        .Include(p => p.Claims.Select(c => c.FinanceOperations))
        .Where(c => c.ProjectId == projectId).LoadAsync();
    }


    protected Task LoadProjectFields(int projectId)
    {
      Debug.WriteLine($"{nameof(LoadProjectFields)} started");
      return Ctx
        .ProjectsSet
        .Include(p => p.ProjectFields)
        .Include(p => p.ProjectFields.Select(pf => pf.DropdownValues))
        .Where(c => c.ProjectId == projectId).LoadAsync();
    }

    protected Task LoadProjectCharactersAndGroups(int projectId)
    {
      Debug.WriteLine($"{nameof(LoadProjectCharactersAndGroups)} started");
      return Ctx.ProjectsSet
        .Include(p => p.CharacterGroups)
        .Include(p => p.Characters)
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }

    protected Task LoadProjectGroups(int projectId)
    {
      Debug.WriteLine($"{nameof(LoadProjectGroups)} started");
      return Ctx.ProjectsSet
        .Include(p => p.CharacterGroups)
        .Where(p => p.ProjectId == projectId)
        .LoadAsync();
    }
  }
}
