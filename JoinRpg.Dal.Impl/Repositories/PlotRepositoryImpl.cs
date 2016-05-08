using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using JoinRpg.Data.Interfaces;
using JoinRpg.DataModel;
using JoinRpg.Helpers;

namespace JoinRpg.Dal.Impl.Repositories
{
  [UsedImplicitly]
  public class PlotRepositoryImpl : RepositoryImplBase, IPlotRepository
  {
    public Task<PlotFolder> GetPlotFolderAsync(int projectId, int plotFolderId)
    {
      return
        Ctx.Set<PlotFolder>()
          .Include(pf => pf.Project)
          .Include(pf => pf.Project.ProjectAcls)
          .Include(pf => pf.Elements)
          .Include(pf => pf.Project.Characters)
          .Include(pf => pf.Project.CharacterGroups)
          .Include(pf => pf.Project.Claims)
          .SingleOrDefaultAsync(pf => pf.PlotFolderId == plotFolderId && pf.ProjectId == projectId);
    }

    public async Task<IReadOnlyCollection<PlotElement>> GetPlotsForCharacter(Character character)
    {
      var ids =
        character.Groups.SelectMany(group => group.FlatTree(g => g.ParentGroups))
          .Select(g => g.CharacterGroupId)
          .Distinct()
          .ToList(); //ToList required here so all lazy loads are finished before we are starting making condition below.
      return
        await Ctx.Set<PlotElement>()
          .Where(
            e =>
              e.TargetCharacters.Any(ch => ch.CharacterId == character.CharacterId) ||
              e.TargetGroups.Any(g => ids.Contains(g.CharacterGroupId)))
          .ToListAsync();
    }

    public Task<List<PlotFolder>> GetPlots(int project)
      => Ctx.Set<PlotFolder>().Include(pf => pf.Elements).Where(pf => pf.ProjectId == project).ToListAsync();

    public Task<List<PlotFolder>> GetPlotsWithTargets(int projectId)
      =>
        Ctx.Set<PlotFolder>()
          .Include(pf => pf.Elements.Select(e => e.TargetCharacters))
          .Include(pf => pf.Elements.Select(e => e.TargetGroups))
          .Where(pf => pf.ProjectId == projectId)
          .ToListAsync();


    public PlotRepositoryImpl(MyDbContext ctx) : base(ctx)
    {
    }
  }
}
