using System.Data.Entity;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using JoinRpg.PrimitiveTypes;

namespace JoinRpg.Dal.Impl.Repositories;

internal class GameRepositoryImplBase : RepositoryImplBase
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
          .Include(p => p.Characters.Select(c => c.Claims))
          .Include(p => p.Characters.Select(c => c.ApprovedClaim))
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

    protected static void EnsureSameProject(IReadOnlyCollection<IProjectEntityId> entityId, [CallerArgumentExpression(nameof(entityId))] string name = "entityId")
    {
        if (entityId.Select(c => c.ProjectId).Distinct().Count() > 1)
        {
            throw new ArgumentException("Нельзя смешивать разные проекты в запросе!", name);
        }
    }
}
